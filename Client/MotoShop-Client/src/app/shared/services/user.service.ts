import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../apiResponse';
import { UpdateUserPasswordModel } from '../models/user/updateUserPassword.model';
import { UpdateUserProfileDataModel } from '../models/user/updateUserProfileData.model';
import { UserProfileDataModel } from '../models/user/userProfileData.model';
import { serverUserProfileDetailsUrl, serverUpdateUserProfileUrl, serverUpdateUserPasswordUrl } from '../server-urls';
import { IdentityService } from './identity.service';

@Injectable()
export class UserService {
    constructor(private httpClient: HttpClient, private identityService: IdentityService) { }

    getUserProfileData(): Observable<ApiResponse<UserProfileDataModel>> | null {
        if (!this.identityService.isSignedIn)
            return null;

        return this.httpClient.get<ApiResponse<UserProfileDataModel>>(serverUserProfileDetailsUrl);
    }
    updateUserProfile(model: UpdateUserProfileDataModel) {
        return this.httpClient.post(serverUpdateUserProfileUrl, model);
    }

    updatePassword(model: UpdateUserPasswordModel) {
        return this.httpClient.post(serverUpdateUserPasswordUrl, model);
    }
}