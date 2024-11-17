import { InjectionToken } from '@angular/core';
import { AppConfig } from '../../_api';

export const APP_CONFIG = new InjectionToken<AppConfig>('app.config');
