import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { slideInOutAnimation } from 'src/app/shared/animations';
import { FormsMapper } from 'src/app/shared/mapper/formsMapper';
import { SignInModel } from 'src/app/shared/models/user/signIn.model';
import { IdentityService } from 'src/app/shared/services/identity.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.sass'],
  encapsulation: ViewEncapsulation.None,
  animations: [
  slideInOutAnimation
  ]
})
export class LoginComponent implements OnInit{
  constructor(private fb: FormBuilder,
    private identityService: IdentityService,
    private mapper: FormsMapper,
    private router: Router) { }

  public showErrorMessage:boolean = false;  
  public showLoadingSpinner: boolean = false;
  public loginForm: FormGroup;
  public animationState: "slideIn" | "slideOut" = "slideIn";
  
   ngOnInit() :void{
    this.loginForm = this.fb.group(
      {
        data: new FormControl('', Validators.required),
        password: ['', Validators.required]
      });
  }

  onSubmit():void{
    this.showErrorMessage = false;
    this.showLoadingSpinner = true;
    let model = this.mapper.map<SignInModel>(new SignInModel(), this.loginForm);

    this.identityService.signIn(model).subscribe(
      (res:any) =>{
        this.showLoadingSpinner = false;
        this.identityService.saveToken(res.Token);
        this.router.navigateByUrl("/home").then(() =>
        {
          window.location.reload();
        });

      },
      (error) => {
        this.showLoadingSpinner = false;
        if(error.status == 404)
        {
          this.showErrorMessage = true;
        }
      }
      
    );

    
  }


}