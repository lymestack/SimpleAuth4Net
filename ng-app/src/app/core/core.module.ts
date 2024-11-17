import { APP_INITIALIZER, NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ShellComponent } from './shell/shell.component';
import { HeaderComponent } from './header/header.component';
import { FooterComponent } from './footer/footer.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { SharedModule } from '../shared/shared.module';
import { RestService } from './_services/rest.service';
import { LoggerService } from './_services/logger.service';
import { HTTP_INTERCEPTORS } from '@angular/common/http';
import { JwtInterceptor } from './jwt-interceptor';

@NgModule({
  declarations: [ShellComponent, HeaderComponent, FooterComponent],
  imports: [CommonModule, BrowserAnimationsModule, SharedModule],
  exports: [ShellComponent],
  providers: [
    {
      provide: HTTP_INTERCEPTORS,
      useClass: JwtInterceptor,
      multi: true,
    },
    LoggerService,
    RestService,
  ],
})
export class CoreModule {}
