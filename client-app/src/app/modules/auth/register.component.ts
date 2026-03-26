import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  template: `
    <div class="auth-container">
      <div class="auth-card">
        <div class="auth-header">
          <h1>Join TrainConnect</h1>
          <p>Create your account to get started</p>
        </div>
        <form (ngSubmit)="onRegister()" class="auth-form">
          <div class="form-group">
            <label for="username">Username</label>
            <input id="username" type="text" [(ngModel)]="username" name="username"
                   placeholder="Choose a username" required minlength="3" maxlength="50"
                   autocomplete="username">
          </div>
          <div class="form-group">
            <label for="displayName">Display Name</label>
            <input id="displayName" type="text" [(ngModel)]="displayName" name="displayName"
                   placeholder="Your display name" required maxlength="100">
          </div>
          <div class="form-group">
            <label for="email">Email</label>
            <input id="email" type="email" [(ngModel)]="email" name="email"
                   placeholder="Enter your email" required autocomplete="email">
          </div>
          <div class="form-group">
            <label for="password">Password</label>
            <input id="password" type="password" [(ngModel)]="password" name="password"
                   placeholder="Min 8 chars, uppercase, lowercase, digit, special" required minlength="8"
                   autocomplete="new-password">
          </div>
          <div *ngIf="error" class="error-message">{{ error }}</div>
          <button type="submit" [disabled]="isLoading" class="btn-primary">
            {{ isLoading ? 'Creating account...' : 'Create Account' }}
          </button>
        </form>
        <p class="auth-footer">
          Already have an account? <a routerLink="/login">Sign In</a>
        </p>
      </div>
    </div>
  `,
  styles: [`
    .auth-container {
      display: flex; justify-content: center; align-items: center;
      min-height: 100vh; background: linear-gradient(135deg, #0f0c29, #302b63, #24243e);
      padding: 20px;
    }
    .auth-card {
      background: rgba(255,255,255,0.05); backdrop-filter: blur(20px);
      border: 1px solid rgba(255,255,255,0.1); border-radius: 20px;
      padding: 40px; width: 100%; max-width: 420px; color: white;
    }
    .auth-header { text-align: center; margin-bottom: 30px; }
    .auth-header h1 { font-size: 1.8em; margin: 0 0 8px; background: linear-gradient(90deg, #00d2ff, #3a7bd5); -webkit-background-clip: text; -webkit-text-fill-color: transparent; }
    .auth-header p { color: rgba(255,255,255,0.6); font-size: 0.9em; margin: 0; }
    .form-group { margin-bottom: 16px; }
    .form-group label { display: block; margin-bottom: 6px; color: rgba(255,255,255,0.7); font-size: 0.85em; }
    .form-group input {
      width: 100%; padding: 12px 16px; border: 1px solid rgba(255,255,255,0.15);
      border-radius: 10px; background: rgba(255,255,255,0.08); color: white;
      font-size: 1em; outline: none; transition: border-color 0.3s; box-sizing: border-box;
    }
    .form-group input:focus { border-color: #3a7bd5; }
    .form-group input::placeholder { color: rgba(255,255,255,0.3); }
    .btn-primary {
      width: 100%; padding: 14px; border: none; border-radius: 10px;
      background: linear-gradient(90deg, #00d2ff, #3a7bd5); color: white;
      font-size: 1em; font-weight: 600; cursor: pointer; transition: opacity 0.3s; margin-top: 8px;
    }
    .btn-primary:hover { opacity: 0.9; }
    .btn-primary:disabled { opacity: 0.5; cursor: not-allowed; }
    .error-message { color: #ff6b6b; font-size: 0.85em; margin-bottom: 12px; text-align: center; }
    .auth-footer { text-align: center; margin-top: 20px; color: rgba(255,255,255,0.5); font-size: 0.9em; }
    .auth-footer a { color: #00d2ff; text-decoration: none; }
  `]
})
export class RegisterComponent {
  username = '';
  displayName = '';
  email = '';
  password = '';
  error = '';
  isLoading = false;

  constructor(private authService: AuthService, private router: Router) {
    if (this.authService.isAuthenticated) {
      this.router.navigate(['/reels']);
    }
  }

  onRegister(): void {
    if (!this.username || !this.email || !this.password || !this.displayName) {
      this.error = 'Please fill in all fields';
      return;
    }
    this.isLoading = true;
    this.error = '';
    this.authService.register({
      username: this.username,
      email: this.email,
      password: this.password,
      displayName: this.displayName
    }).subscribe({
      next: () => {
        this.router.navigate(['/reels']);
      },
      error: (err) => {
        this.error = err.error?.error || err.error?.errors?.Password?.[0] || 'Registration failed. Please try again.';
        this.isLoading = false;
      }
    });
  }
}
