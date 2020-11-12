import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { HomeComponent } from './home/home.component';
import { ConfirmationComponent } from './identity/confirmation-component/confirmation-component.component';
import { IdentityPlaceholderComponent } from './identity/identity-placeholder/identity-placeholder.component';
import { LoginComponent } from './identity/login/login.component';
import { RegisterComponent } from './identity/register/register.component';
import { UserProfileComponent } from './identity/user-profile/user-profile.component';
import { AuthenticationGuard } from './shared/Guards/authentication.guard';


const routes: Routes = [
  {
    path: "",
    component: HomeComponent
  },
  {
    path: "home",
    component: HomeComponent
  },
  {
    path: "identity",
    component: IdentityPlaceholderComponent,
    children: [
      {
        path: '',
        component: LoginComponent,
        outlet: "identity"
      },
      {
        path: 'register',
        component: RegisterComponent,
        outlet: "identity"
      },
      {
        path: 'login',
        component: LoginComponent,
        outlet: "identity"
      }
    ]
  },
  {
    path: "userProfile",
    component: UserProfileComponent,
    canActivate: [AuthenticationGuard]
  },
  {
    path: "confirmation/:type?",
    component: ConfirmationComponent
  }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }
