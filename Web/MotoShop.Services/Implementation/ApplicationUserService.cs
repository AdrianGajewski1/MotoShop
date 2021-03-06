﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MotoShop.Data.Database_Context;
using MotoShop.Data.Helpers;
using MotoShop.Data.Models.User;
using MotoShop.Services.Extensions;
using MotoShop.Services.HelperModels;
using MotoShop.Services.Services;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MotoShop.Services.Implementation
{
    public class ApplicationUserService : IApplicationUserService
    {
        private readonly ApplicationDatabaseContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailSenderService _emailSenderService;
        private readonly IImageUploadService _imageUploadService;

        public ApplicationUserService(ApplicationDatabaseContext dbContext, UserManager<ApplicationUser> userManager,
                                        IConfiguration configuration, IEmailSenderService emailSenderService, IImageUploadService imageUploadService)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _configuration = configuration;
            _emailSenderService = emailSenderService;
            _imageUploadService = imageUploadService;
        }
        public async Task<ApplicationUser> GetUserByEmail(string email) => await _userManager.FindByEmailAsync(email);
        public async Task<ApplicationUser> GetUserByID(string id) => await _userManager.FindByIdAsync(id);
        public async Task<ApplicationUser> GetUserByUserName(string username) => await _userManager.FindByNameAsync(username);
        public async Task<ApplicationUser> RegisterNewUserAsync(ApplicationUser user, string password)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.IsExternal = false;
            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                await _dbContext.SaveChangesAsync();
                return user;
            }

            return null;
        }
        public async Task<string> SignInAsync(string data, string password, UserSignInVariant variant)
        {
            string userID = string.Empty;
            switch (variant)
            {
                case UserSignInVariant.UserName:
                    {
                        var user = await _userManager.FindByNameAsync(data);
                        if(await _userManager.CheckPasswordAsync(user, password))
                        {
                            userID = user.Id;
                        }
                    }
                    break;
                case UserSignInVariant.Email:
                    {
                        var user = await _userManager.FindByEmailAsync(data);
                        if (await _userManager.CheckPasswordAsync(user, password))
                        {
                            userID = user.Id;
                        }
                    }
                    break;
            }

            return userID;
        }
        //TODO: Add email notifications about the changes
        //  *In email editing case, we should first send confirmation email to the old email adress
        public async Task<UpdateResult> UpdateUserDataAsync(string userID, ApplicationUser model)
        {
            ApplicationUser user = await GetUserByID(userID);

            if (user == null)
                return new UpdateResult 
                {
                    Result = false
                };

            var dataToUpdate = ChechWhatDataToUpdate(model);

            if(dataToUpdate.Count == 0)
                return new UpdateResult
                {
                    Result = false
                };

            foreach (var data in dataToUpdate)
            {
                switch (data)
                {
                    case UpdateDataType.UserName:
                        {
                            if (UserExists(model) != 2) 
                            {
                                user.UserName = model.UserName;
                                await _userManager.UpdateNormalizedUserNameAsync(user);
                            }
                            else
                                return new UpdateResult
                                {
                                    ErrorIdentificator = 2,
                                    Result = false
                                };
                                
                        }
                        break;
                    case UpdateDataType.Email:
                        {
                            if (UserExists(model) != 1)
                            {
                                string token = await GenerateUserEmailChangeTokenAsync(user, model.Email);
                                string confirmationLink = GenerateConfirmationLink(token,user.Id,model.Email, UpdateDataType.Email);
                                await _emailSenderService.SendConfirmationEmailAsync(new EmailAddress
                                {
                                    Email = user.Email,
                                    Name = user.UserName
                                }, "Email Verification", confirmationLink, EmailType.Verification_Email_Change);
                            }
                            else
                                return new UpdateResult
                                {
                                    ErrorIdentificator = 1,
                                    Result = false
                                };
                        }

                        break;
                    case UpdateDataType.PhoneNumber:
                        user.PhoneNumber = model.PhoneNumber;
                        break;
                    case UpdateDataType.Name:
                        user.Name = model.Name;
                        break;
                    case UpdateDataType.Lastname:
                        user.LastName = model.LastName;
                        break;
                }
            }
            _dbContext.Entry(user).State = EntityState.Modified;
            var result = await _dbContext.SaveChangesAsync();
            if (result < 0)
                return new UpdateResult
                {
                    Result = false
                };


            return new UpdateResult
            {
                Result = true
            };
        }
        /// <summary>
        /// Returns if UserName or Email is already taken
        /// </summary>
        /// <param name="user">\The user</param>
        /// <returns>0 if email and username are not taken</returns>
        /// <returns>1 if email is taken</returns>
        /// <returns>2 if username is taken</returns>
        public int UserExists(ApplicationUser user)
        {
            int result = 0;
            if (_dbContext.Users.Where(x => x.Email == user.Email).Count() > 0)
                result = 1;

            if (_dbContext.Users.Where(x => x.UserName == user.UserName).Count() > 0)
                result = 2;

            return result;
        }
        private List<UpdateDataType> ChechWhatDataToUpdate(ApplicationUser user)
        {
            List<UpdateDataType> dataTypes = new List<UpdateDataType>();

            if (!string.IsNullOrEmpty(user.Email))
                dataTypes.Add(UpdateDataType.Email);
            if (!string.IsNullOrEmpty(user.UserName))
                dataTypes.Add(UpdateDataType.UserName);
            if (!string.IsNullOrEmpty(user.Name))
                dataTypes.Add(UpdateDataType.Name);
            if (!string.IsNullOrEmpty(user.LastName))
                dataTypes.Add(UpdateDataType.Lastname);
            if (!string.IsNullOrEmpty(user.PhoneNumber))
                dataTypes.Add(UpdateDataType.PhoneNumber);

            return dataTypes;
            
        }
        private async Task<string> GenerateUserEmailChangeTokenAsync(ApplicationUser user, string newEmail)
        {
            string token = await _userManager.GenerateChangeEmailTokenAsync(user, newEmail);
            string encodedToken = EncodeToken(token);

            return encodedToken;
        }
        public async Task<bool> UpdateEmailAsync(ApplicationUser user, string token, string newEmail)
        {
            string tk = DecodeToken(token);
            var updateResult = await _userManager.ChangeEmailAsync(user, newEmail, tk);

            if (updateResult.Succeeded)
                return true;

            return false;
        }
        public async Task<bool> SendAccountConfirmationMessageAsync(ApplicationUser user)
        {
            string token = EncodeToken(await _userManager.GenerateEmailConfirmationTokenAsync(user));
            string link = GenerateConfirmationLink(token, user.Id, null, UpdateDataType.None);
            var result = await _emailSenderService.SendConfirmationEmailAsync(new EmailAddress(user.Email,user.UserName),"Email confirmation",link, EmailType.Verification_Account_Creating);

            return result;
        }
        public async Task<bool> ConfirmUserEmailAsync(ApplicationUser user, string token)
        {
            var tk = DecodeToken(token);
            var result = await _userManager.ConfirmEmailAsync(user, tk);

            return result.Succeeded;
        }
        public string GenerateConfirmationLink(string token, string userID, string newData, UpdateDataType updateDataType = UpdateDataType.None)
        {
            string dataType = updateDataType.ToString();
            string baseUrl = _configuration["ApplicationUrls:HTTPS"];

            var link = new Uri(baseUrl).Append("/api/", "userAccount/", "verificationCallback").AbsoluteUri;
            link = $"{link}?userID={userID}&token={token}";

            if (updateDataType != UpdateDataType.None)
                link += $"&dataType={dataType}&newData={newData}";
            return link;
        }
        public string EncodeToken(string token)
        {
            byte[] tokenInBytes = Encoding.UTF8.GetBytes(token);
            var encodedToken = WebEncoders.Base64UrlEncode(tokenInBytes);

            return encodedToken;
        }
        public string DecodeToken(string token)
        {
            byte[] decodedToken = WebEncoders.Base64UrlDecode(token);
            string tk = Encoding.UTF8.GetString(decodedToken);

            return tk;
        }
        public async Task<bool> UpdatePasswordAsync(ApplicationUser user,string token, string newPassword)
        {
            string tk = DecodeToken(token);
            var resetPasswordResult = await _userManager.ResetPasswordAsync(user, tk, newPassword);

            return resetPasswordResult.Succeeded ? true : false;
        }
        public async Task<bool> SendPasswordChangingConfirmationMessageAsync(ApplicationUser user, string newPassword)
        {
            string token = EncodeToken(await _userManager.GeneratePasswordResetTokenAsync(user));
            string link = GenerateConfirmationLink(token, user.Id, newPassword, UpdateDataType.Password);

            return await _emailSenderService.SendConfirmationEmailAsync(new EmailAddress(user.Email, user.UserName), "Password Reset", link, EmailType.Verification_PasswordChange);
        }
        public async Task<bool> AddUserProfileImageAsync(string userID, string path)
        {
            ApplicationUser user = await GetUserByID(userID);

            if (!string.IsNullOrEmpty(user.ImageUrl))
                _imageUploadService.DeleteImage(user.ImageUrl);

            user.ImageUrl = path;
            _dbContext.Entry(user).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            var result = await _dbContext.SaveChangesAsync();

            if (result > 0)
                return true;

            return false;
        }
        public async Task<bool> UserExists(string email, string username)
        {
            var user = await GetUserByEmail(email);

            return user != null;
        }
        public async Task<bool> IsAdmin(string userID)
        {
            return await _userManager.IsInRoleAsync(await GetUserByID(userID), ApplicationRoles.Administrator);
        }
        public async Task<IEnumerable<string>> GetUserRolesAsync(string userID)
        {
            return await _userManager.GetRolesAsync(await GetUserByID(userID));
        }
        public async Task<bool> DeleteUser(string userID)
        {
            var user = await GetUserByID(userID);

            if (!await UserExists(user.Email, user.UserName))
                return false;

            _dbContext.Users.Remove(user);

            var result = await _dbContext.SaveChangesAsync();

            return result > 0;
        }
        public async Task<TResult> GetUserData<TResult>(string userID, Func<ApplicationUser, TResult> selectExpression)
        {
            return selectExpression(await GetUserByID(userID));
        }
    }
}
