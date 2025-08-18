import { Inject, Injectable, Optional } from '@angular/core';
import { APP_CONFIG } from './config-injection';
import { AppConfig, PasswordComplexityOptions } from '../../_api';

export interface PasswordValidationResult {
  valid: boolean;
  errors: string[];
}

@Injectable({
  providedIn: 'root'
})
export class PasswordService {
  private passwordRequirements: PasswordComplexityOptions = {
    requiredLength: 8,
    requiredUniqueChars: 4,
    requireDigit: true,
    requireLowercase: true,
    requireUppercase: true,
    requireNonAlphanumeric: true
  };

  constructor(@Optional() @Inject(APP_CONFIG) appConfig?: AppConfig) {
    if (appConfig?.simpleAuth?.passwordComplexity) {
      this.passwordRequirements = appConfig.simpleAuth.passwordComplexity;
    }
  }

  getRequirements(): PasswordComplexityOptions {
    return { ...this.passwordRequirements };
  }

  generatePassword(): string {
    const uppercase = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
    const lowercase = 'abcdefghijklmnopqrstuvwxyz';
    const digits = '0123456789';
    const special = '!@#$%^&*()_-';
    
    const password: string[] = [
      uppercase[Math.floor(Math.random() * uppercase.length)],
      lowercase[Math.floor(Math.random() * lowercase.length)],
      digits[Math.floor(Math.random() * digits.length)],
      special[Math.floor(Math.random() * special.length)]
    ];
    
    const allChars = uppercase + lowercase + digits + special;
    const remainingLength = 12 - password.length;
    
    for (let i = 0; i < remainingLength; i++) {
      password.push(allChars[Math.floor(Math.random() * allChars.length)]);
    }
    
    for (let i = password.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [password[i], password[j]] = [password[j], password[i]];
    }
    
    const finalPassword = password.join('');
    
    const uniqueChars = new Set(finalPassword).size;
    if (uniqueChars < this.passwordRequirements.requiredUniqueChars) {
      return this.generatePassword();
    }
    
    return finalPassword;
  }

  validatePassword(password: string | null | undefined): PasswordValidationResult {
    const errors: string[] = [];
    
    if (!password) {
      errors.push('Password cannot be empty.');
      return { valid: false, errors };
    }
    
    if (password.length < this.passwordRequirements.requiredLength) {
      errors.push(`Password must be at least ${this.passwordRequirements.requiredLength} characters long.`);
    }
    
    if (this.passwordRequirements.requireDigit && !password.match(/[0-9]/)) {
      errors.push('Password must contain at least one numeric digit.');
    }
    
    if (this.passwordRequirements.requireLowercase && !password.match(/[a-z]/)) {
      errors.push('Password must contain at least one lowercase letter.');
    }
    
    if (this.passwordRequirements.requireUppercase && !password.match(/[A-Z]/)) {
      errors.push('Password must contain at least one uppercase letter.');
    }
    
    if (this.passwordRequirements.requireNonAlphanumeric && !password.match(/[^a-zA-Z0-9]/)) {
      errors.push('Password must contain at least one non-alphanumeric character.');
    }
    
    const uniqueChars = new Set(password).size;
    if (uniqueChars < this.passwordRequirements.requiredUniqueChars) {
      errors.push(`Password must contain at least ${this.passwordRequirements.requiredUniqueChars} unique characters.`);
    }
    
    return {
      valid: errors.length === 0,
      errors
    };
  }

  getPasswordHint(): string {
    const hints: string[] = [];
    
    hints.push(`${this.passwordRequirements.requiredLength}+ chars`);
    
    if (this.passwordRequirements.requireUppercase) hints.push('uppercase');
    if (this.passwordRequirements.requireLowercase) hints.push('lowercase');
    if (this.passwordRequirements.requireDigit) hints.push('digit');
    if (this.passwordRequirements.requireNonAlphanumeric) hints.push('special (!@#$%^&*()_-)');
    if (this.passwordRequirements.requiredUniqueChars > 1) {
      hints.push(`${this.passwordRequirements.requiredUniqueChars}+ unique`);
    }
    
    return hints.join(', ');
  }

  updateRequirements(requirements: Partial<PasswordComplexityOptions>): void {
    this.passwordRequirements = {
      ...this.passwordRequirements,
      ...requirements
    };
  }
}