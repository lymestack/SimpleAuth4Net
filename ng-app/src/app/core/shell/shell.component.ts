import { Component, OnInit, ViewChild } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Observable } from 'rxjs';
import { map, shareReplay } from 'rxjs/operators';
import { MatDrawer } from '@angular/material/sidenav';
import { CurrentUserService } from '../_services/current-user.service';
import { AppUser } from '../../_api';
import { LoggerService } from '../_services/logger.service';
import { Router } from '@angular/router';
import { AuthService } from '../_services/auth.service';
@Component({
  selector: 'app-shell',
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
})
export class ShellComponent implements OnInit {
  pageTitle = '';
  isHandset$: Observable<boolean>;
  @ViewChild('drawer') drawer!: MatDrawer;
  appUser: AppUser | null = null;
  isAdmin = false;

  constructor(
    private authService: AuthService,
    private breakpointObserver: BreakpointObserver,
    private currentUser: CurrentUserService,
    private logger: LoggerService,
    private router: Router
  ) {
    this.isHandset$ = this.breakpointObserver.observe(Breakpoints.Handset).pipe(
      map((result) => result.matches),
      shareReplay()
    );
  }

  ngOnInit(): void {
    if (this.authService.isLoggedIn()) {
      console.log('User is logged in. Validating token.');

      // Attempt to refresh the token if necessary
      this.authService.refreshToken().subscribe(
        (response) => {
          console.log('Token refreshed successfully.', response);
          this.loadCurrentUser(); // Load user data after successful token refresh
        },
        (error) => {
          this.logger.error('Token refresh failed. Logging out.', error);
          this.authService.logout();
          this.router.navigate(['/account/login']);
        }
      );

      this.isAdmin = this.currentUser.isInRole('Admin');
    } else {
      console.log('User is not logged in.');
      this.appUser = null;
    }
  }

  private loadCurrentUser(): void {
    this.currentUser.getAppUser().subscribe(
      (data: AppUser) => {
        this.appUser = data;
        console.log('Loaded current user:', data);
      },
      (error) => {
        this.logger.error('Failed to load current user:', error);
        this.appUser = null;
      }
    );
  }

  toggleDrawer(): void {
    this.drawer.toggle(); // Safely toggles the drawer
  }
}
