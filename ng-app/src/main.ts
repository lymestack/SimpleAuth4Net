import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
import { AppModule } from './app/app.module';
import { APP_CONFIG } from './app/core/_services/config-injection';

loadAppConfig()
  .then((config) => {
    return platformBrowserDynamic([
      { provide: APP_CONFIG, useValue: config },
    ]).bootstrapModule(AppModule, {
      ngZoneEventCoalescing: true,
    });
  })
  .catch((err) => {
    console.error('Unable to retrieve AppConfig:', err);
    showBootstrapError();
  });

function loadAppConfig(): Promise<any> {
  const url = getConfigUrl();
  return fetch(url).then((res) => {
    if (!res.ok) throw new Error(`HTTP error ${res.status}`);
    return res.json();
  });
}

function getConfigUrl(): string {
  const href = window.location.href;

  if (href.includes('uat.yourdomain.com'))
    return 'https://uat.yourdomain.com/api/AppConfig';

  if (href.includes('yourdomain.com'))
    return 'https://www.yourdomain.com/api/AppConfig';

  // Toggle this manually depending on IIS vs Kestrel
  const useIIS = true;

  return useIIS
    ? 'http://localhost/SimpleAuthNet/api/AppConfig'
    : 'http://localhost:5218/AppConfig';
}

function showBootstrapError(): void {
  document.body.innerHTML = `
    <div style="text-align:center;margin-top:100px;">
      <h2 style="color:red;">Unable to retrieve app configuration data</h2>
      <p>Please check your internet connection or try again later.</p>
    </div>
  `;
}
