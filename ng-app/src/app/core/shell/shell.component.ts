import { Component, OnInit, ViewChild } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Observable } from 'rxjs';
import { map, shareReplay } from 'rxjs/operators';
import { MatDrawer } from '@angular/material/sidenav';
import { CurrentUserService } from '../_services/current-user.service';
import { AppUser } from '../../_api';
@Component({
  selector: 'app-shell',
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent implements OnInit {
  isHandset$: Observable<boolean>;
  @ViewChild('drawer') drawer!: MatDrawer;
  appUser: AppUser;

  constructor(
    private breakpointObserver: BreakpointObserver,
    private currentUser: CurrentUserService
  ) {
    this.isHandset$ = this.breakpointObserver.observe(Breakpoints.Handset).pipe(
      map((result) => result.matches),
      shareReplay()
    );
  }

  ngOnInit(): void {
    this.currentUser
      .getAppUser()
      .subscribe((data: AppUser) => (this.appUser = data));
  }

  logout() {
    console.log('Logging out...');
    // Add your logout logic here
  }

  toggleDrawer(): void {
    this.drawer.toggle(); // Safely toggles the drawer
  }
}
