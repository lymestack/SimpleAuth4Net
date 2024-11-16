import { Component } from '@angular/core';
import { routerTransition } from './core/router.animation';

@Component({
  selector: 'app-root',
  animations: [routerTransition],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent {
  isIframe = false;

  ngOnInit() {
    this.isIframe = window !== window.parent && !window.opener;
  }

  getState(outlet: any) {
    return outlet.activatedRouteData.state;
  }
}
