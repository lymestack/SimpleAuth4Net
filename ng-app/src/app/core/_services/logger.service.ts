import { Injectable } from '@angular/core';
import {
  MatSnackBar,
  MatSnackBarConfig,
  MatSnackBarRef,
} from '@angular/material/snack-bar';
import { SnackBarTemplateComponent } from '../../shared/material/snack-bar-template/snack-bar-template.component';

export class LoggerModel {
  message: string = '';
  title?: string;
  icon?: string;
  messageType: string = 'info';
}

@Injectable()
export class LoggerService {
  snackBarRef: MatSnackBarRef<any> | undefined;
  duration = 5000;

  constructor(private snackBarService: MatSnackBar) {}

  success(
    message: string,
    title: string = 'Success',
    durationMilliseconds = this.duration
  ) {
    this.openSnackBar(message, title, 'success', durationMilliseconds);
  }

  info(
    message: string,
    title: string = 'Info',
    durationMilliseconds = this.duration
  ) {
    this.openSnackBar(message, title, 'info', durationMilliseconds);
  }

  warning(
    message: string,
    title: string = 'Warning',
    durationMilliseconds = this.duration
  ) {
    this.openSnackBar(message, title, 'warning', durationMilliseconds);
  }

  error(message: string, title: string = 'Error', durationMilliseconds = 5000) {
    this.openSnackBar(message, title, 'error', durationMilliseconds);
  }

  private openSnackBar(
    message: string,
    title: string,
    messageType: string,
    durationMilliseconds: number
  ) {
    let icon: string;
    switch (messageType) {
      case 'info':
        icon = 'fa-circle-info';
        break;
      case 'warning':
        icon = 'fa-exclamation-triangle';
        break;
      case 'error':
        icon = 'fa-times-circle';
        break;
      case 'success':
        icon = 'fa-check-circle';
        break;
      default:
        icon = 'fa-info-circle';
    }

    let loggerModel: LoggerModel = {
      message: message,
      title: title,
      icon: icon,
      messageType: messageType,
    };

    let config: MatSnackBarConfig = {
      duration: durationMilliseconds,
      panelClass: 'snackbar-' + messageType,
      horizontalPosition: 'right',
      verticalPosition: 'bottom',
      data: loggerModel,
    };

    this.snackBarRef = this.snackBarService.openFromComponent(
      SnackBarTemplateComponent,
      config
    );
  }
}
