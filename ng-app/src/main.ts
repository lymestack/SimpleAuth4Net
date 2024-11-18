import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
import { AppModule } from './app/app.module';
import { APP_CONFIG } from './app/core/_services/config-injection';

fetch(getConfigUrl())
  .then((res) => res.json())
  .then((config) => {
    platformBrowserDynamic([{ provide: APP_CONFIG, useValue: config }])
      .bootstrapModule(AppModule, {
        ngZoneEventCoalescing: true,
      })
      .catch((err) => {
        console.error(err);
        alert("Couldn't load config. Try again");
      });
  });

function getConfigUrl() {
  if (window.location.href.includes('uat.yourdomain.com'))
    return 'https://uat.yourdomain.com/api/AppConfig';

  if (window.location.href.includes('yourdomain.com'))
    return 'https://www.yourdomain.com/api/AppConfig';

  // ZOMBIE - Use for IIS:
  return 'http://localhost/SimpleAuthNet/api/AppConfig';

  // Kestrel:
  return 'https://localhost:7214/AppConfig';
}
