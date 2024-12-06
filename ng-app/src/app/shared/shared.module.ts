import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MaterialModule } from './material/material.module';
import { CardMenuComponent } from './card-menu/card-menu.component';
import { RouterModule } from '@angular/router';
import { HttpClientModule } from '@angular/common/http';

@NgModule({
  declarations: [CardMenuComponent],
  imports: [
    CommonModule,
    FormsModule,
    HttpClientModule,
    MaterialModule,
    RouterModule,
  ],
  exports: [
    FormsModule,
    HttpClientModule,
    MaterialModule,
    RouterModule,
    CardMenuComponent,
  ],
})
export class SharedModule {}
