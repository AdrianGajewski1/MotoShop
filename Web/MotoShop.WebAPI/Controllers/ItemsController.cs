﻿using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using MotoShop.Data.Models.Constants;
using MotoShop.Data.Models.Store;
using MotoShop.Services.Services;
using MotoShop.WebAPI.Attributes;
using MotoShop.WebAPI.Helpers;
using MotoShop.WebAPI.Models.Response;
using MotoShop.WebAPI.Models.Response.ItemsController;
using System;

namespace MotoShop.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemsController : ControllerBase
    {
        private readonly IShopItemsService _shopItemsService;
        private readonly IMapper _mapper;

        public ItemsController(IShopItemsService shopItemsService, IMapper mapper)
        {
            _shopItemsService = shopItemsService;
            _mapper = mapper;
        }

        [HttpGet("Details/{id}")]
        [Cache(5)]
        public ActionResult<ApiResponse<ItemDetailsResponseModel>> GetItemDetails(int id, [FromQuery] string itemType)
        {
            ShopItem item = _shopItemsService.GetItemByID(id, Enum.Parse<ItemType>(itemType));

            if (item == null)
                return NotFound(StaticMessages.NotFound("Item", "id", id));

            ItemDetailsResponseModel model = null;

            if(item.GetType() == typeof(Car))
            {
                Car car = item as Car;

                model = _mapper.Map<CarDetailsResponseModel>(car);

                ApiResponse<CarDetailsResponseModel> responseModel = new ApiResponse<CarDetailsResponseModel>
                {
                    HttpStatusCode = System.Net.HttpStatusCode.OK,
                    ResponseContent = (CarDetailsResponseModel)model
                };

                return Ok(responseModel);
            }

            var respone = new ApiResponse<string>
            {
                HttpStatusCode = System.Net.HttpStatusCode.InternalServerError,
                ResponseContent = StaticMessages.SomethingWentWrong
            };

            return BadRequest(respone);
        }
    }
}
