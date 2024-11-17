import { Component, Inject } from '@angular/core';
import { MAT_SNACK_BAR_DATA } from '@angular/material/snack-bar';

export interface CustomSnackBarData {
  message: string;
  snackType: 'info' | 'warning' | 'danger' | 'success' | 'secondary';
  icon?: string;
}

@Component({
  selector: 'app-custom-snackbar',
  templateUrl: './custom-snack-bar.component.html',
  styleUrls: ['./custom-snack-bar.component.scss'],
})
export class CustomSnackBarComponent {
  message: string;
  snackType: string;
  icon?: string;

  constructor(@Inject(MAT_SNACK_BAR_DATA) public data: CustomSnackBarData) {
    this.message = data.message;
    this.snackType = data.snackType;
    this.icon = data.icon;
  }
}
