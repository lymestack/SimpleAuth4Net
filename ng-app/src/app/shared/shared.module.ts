import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MaterialModule } from './material/material.module';
import { CardMenuComponent } from './card-menu/card-menu.component';
import { RouterModule } from '@angular/router';
import { HttpClientModule } from '@angular/common/http';
import { SortableColumnLabelComponent } from './sortable-column-label/sortable-column-label.component';

@NgModule({
  declarations: [CardMenuComponent, SortableColumnLabelComponent],
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
    SortableColumnLabelComponent,
  ],
})
export class SharedModule {}
