import { Component, ViewChild } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Observable } from 'rxjs';
import { map, shareReplay } from 'rxjs/operators';
import { MatDrawer } from '@angular/material/sidenav';
@Component({
  selector: 'app-shell',
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent {
  isHandset$: Observable<boolean>;
  @ViewChild('drawer') drawer!: MatDrawer;

  constructor(private breakpointObserver: BreakpointObserver) {
    this.isHandset$ = this.breakpointObserver.observe(Breakpoints.Handset).pipe(
      map((result) => result.matches),
      shareReplay()
    );
  }

  logout() {
    console.log('Logging out...');
    // Add your logout logic here
  }

  toggleDrawer(): void {
    this.drawer.toggle(); // Safely toggles the drawer
  }
}
