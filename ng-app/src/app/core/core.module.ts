import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ShellComponent } from './shell/shell.component';
import { HeaderComponent } from './header/header.component';
import { FooterComponent } from './footer/footer.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { SharedModule } from '../shared/shared.module';
import { RestService } from './_services/rest.service';
import { CustomSnackBarComponent } from './custom-snack-bar/custom-snack-bar.component';

@NgModule({
  declarations: [ShellComponent, HeaderComponent, FooterComponent, CustomSnackBarComponent],
  imports: [CommonModule, BrowserAnimationsModule, SharedModule],
  exports: [ShellComponent],
  providers: [RestService],
})
export class CoreModule {}
