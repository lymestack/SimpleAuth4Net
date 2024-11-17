import { Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import {
  CustomSnackBarComponent,
  CustomSnackBarData,
} from '../custom-snack-bar/custom-snack-bar.component';

@Injectable({
  providedIn: 'root',
})
export class SnackbarService {
  constructor(private snackBar: MatSnackBar) {}

  //   openSnackBar(
  //     message: string,
  //     snackType: 'info' | 'warning' | 'danger' | 'success' | 'secondary',
  //     icon?: string,
  //     duration: number = 3000
  //   ) {
  //     this.snackBar.openFromComponent(CustomSnackBarComponent, {
  //       data: { message, snackType, icon } as CustomSnackBarData,
  //       duration,
  //       horizontalPosition: 'right',
  //       verticalPosition: 'top',
  //       panelClass: `snack-bar-${snackType}`,
  //     });
  //   }

  openSnackBar(
    message: string,
    snackType: 'info' | 'warning' | 'danger' | 'success' | 'secondary',
    icon?: string,
    duration: number = 6000
  ) {
    let panelClass = `snack-bar-${snackType}`;
    debugger;
    this.snackBar.openFromComponent(CustomSnackBarComponent, {
      data: { message, snackType, icon } as CustomSnackBarData,
      duration,
      horizontalPosition: 'right',
      verticalPosition: 'top',
      panelClass: panelClass, // Dynamically add class
    });
  }
}
