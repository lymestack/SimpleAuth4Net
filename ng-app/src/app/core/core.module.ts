import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ShellComponent } from './shell/shell.component';
import { HeaderComponent } from './header/header.component';
import { FooterComponent } from './footer/footer.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { SharedModule } from '../shared/shared.module';

@NgModule({
  declarations: [ShellComponent, HeaderComponent, FooterComponent],
  imports: [CommonModule, BrowserAnimationsModule, SharedModule],
  exports: [ShellComponent],
})
export class CoreModule {}
