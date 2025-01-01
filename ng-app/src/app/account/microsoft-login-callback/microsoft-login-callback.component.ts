import { Component } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../core/_services/auth.service';

@Component({
  selector: 'app-microsoft-login-callback',
  templateUrl: './microsoft-login-callback.component.html',
  styleUrl: './microsoft-login-callback.component.scss',
})
export class MicrosoftLoginCallbackComponent {
  constructor(
    private route: ActivatedRoute,
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe((params) => {
      const code = params['code'];
      if (code) {
        this.authService.loginWithMicrosoft(code).subscribe(
          () => {
            console.log('Logged in with Microsoft successfully.');
            this.router.navigate(['/']); // Redirect after login
          },
          (error) => {
            console.error('Microsoft login failed:', error);
          }
        );
      } else {
        console.error('No authorization code found in URL.');
      }
    });
  }
}
