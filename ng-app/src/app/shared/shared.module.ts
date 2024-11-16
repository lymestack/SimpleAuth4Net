import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MaterialModule } from './material-module/material.module';

@NgModule({
  declarations: [],
  imports: [CommonModule, FormsModule, MaterialModule],
  exports: [FormsModule, MaterialModule],
})
export class SharedModule {}
