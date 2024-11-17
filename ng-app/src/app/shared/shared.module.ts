import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MaterialModule } from './material/material.module';
import { CardMenuComponent } from './card-menu/card-menu.component';
import { RouterModule } from '@angular/router';
import { RecaptchaModule } from 'ng-recaptcha';
import { UsernameEmailInputComponent } from './username-email-input/username-email-input.component';
import { HttpClientModule } from '@angular/common/http';

@NgModule({
  declarations: [CardMenuComponent, UsernameEmailInputComponent],
  imports: [
    CommonModule,
    FormsModule,
    HttpClientModule,
    MaterialModule,
    RouterModule,
    RecaptchaModule,
  ],
  exports: [
    FormsModule,
    HttpClientModule,
    MaterialModule,
    RouterModule,
    RecaptchaModule,
    CardMenuComponent,
    UsernameEmailInputComponent,
  ],
})
export class SharedModule {}
