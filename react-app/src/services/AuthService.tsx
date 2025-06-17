import axios from 'axios';

export interface LoginModel {
  username: string;
  password: string;
  deviceId: string;
}

export interface LoginWithGoogleModel {
  credentialsFromGoogle: string;
  deviceId: string;
}

export interface RegisterModel {
  firstName: string;
  lastName: string;
  emailAddress: string;
  username: string;
  password: string;
  confirmPassword: string;
}

class AuthService {
  // private apiUrl: string = 'http://localhost/SimpleAuthNet/api/'; <-- Use this for IIS
  private apiUrl: string = 'http://localhost:5218/';
  private deviceId: string = this.getOrGenerateDeviceId();

  public setApiUrl(url: string) {
    this.apiUrl = url.endsWith('/') ? url : url + '/';
  }

  public getDeviceId(): string {
    return this.deviceId;
  }

  public getApiUrl(): string {
    return this.apiUrl;
  }

  // Store token expiration in local storage
  private storeTokenExpiration(accessTokenExpires: string, refreshTokenExpires?: string): void {
    const accessExpirationTime = Date.parse(accessTokenExpires);
    if (!isNaN(accessExpirationTime)) {
      localStorage.setItem('accessTokenExpiration', accessExpirationTime.toString());
    }
    if (refreshTokenExpires) {
      const refreshExpirationTime = Date.parse(refreshTokenExpires);
      if (!isNaN(refreshExpirationTime)) {
        localStorage.setItem('refreshTokenExpiration', refreshExpirationTime.toString());
      }
    }
  }

  private getStoredTokenExpiration(): number | null {
    const expiration = localStorage.getItem('accessTokenExpiration');
    return expiration ? parseInt(expiration, 10) : null;
  }

  private getStoredRefreshTokenExpiration(): number | null {
    const expiration = localStorage.getItem('refreshTokenExpiration');
    return expiration ? parseInt(expiration, 10) : null;
  }

  public async login(loginModel: LoginModel): Promise<any> {
    try {
      const response = await axios.post(`${this.apiUrl}Auth/Login`, loginModel, {
        withCredentials: true,
      });
      this.storeTokenExpiration(response.data.expires, response.data.refreshTokenExpires);
      await this.storeUserProfile();
      return response.data;
    } catch (error) {
      console.error('Error during login:', error);
      throw error;
    }
  }

  public async loginWithGoogle(credentialsFromGoogle: string): Promise<any> {
    try {
      const response = await axios.post(
        `${this.apiUrl}Auth/LoginWithGoogle`,
        {
          credentialsFromGoogle,
          deviceId: this.deviceId,
        } as LoginWithGoogleModel,
        { withCredentials: true }
      );
      this.storeTokenExpiration(response.data.expires, response.data.refreshTokenExpires);
      await this.storeUserProfile();
      return response.data;
    } catch (error) {
      console.error('Error during Google login:', error);
      throw error;
    }
  }

  public async refreshToken(): Promise<any> {
    try {
      const response = await axios.get(`${this.apiUrl}Auth/RefreshToken?deviceId=${this.deviceId}`, {
        withCredentials: true,
      });
      this.storeTokenExpiration(response.data.expires, response.data.refreshTokenExpires);
      return response.data;
    } catch (error) {
      console.error('Error refreshing token:', error);
      throw error;
    }
  }

  public async logout(): Promise<void> {
    try {
      await axios.delete(`${this.apiUrl}Auth/Logout`, { withCredentials: true });
      localStorage.removeItem('accessTokenExpiration');
      localStorage.removeItem('refreshTokenExpiration');
      localStorage.removeItem('deviceId');
      localStorage.removeItem('AppUser');
      alert('Logged out successfully.');
    } catch (error) {
      console.error('Error during logout:', error);
      alert('Error logging out. Please try again.');
    }
  }
  

  public isLoggedIn(): boolean {
    const now = new Date().getTime();
    const accessTokenExpires = this.getStoredTokenExpiration();
    const refreshTokenExpires = this.getStoredRefreshTokenExpiration();

    if (accessTokenExpires && now < accessTokenExpires) {
      return true;
    }
    if (refreshTokenExpires && now < refreshTokenExpires) {
      console.log('Access token expired but refresh token is valid.');
      return true;
    }
    return false;
  }

  public async register(model: RegisterModel): Promise<any> {
    const response = await axios.post(`${this.apiUrl}Auth/Register`, model, {
      withCredentials: true,
    });
    return response.data;
  }

  public async userExists(username: string): Promise<boolean> {
    const response = await axios.get(
      `${this.apiUrl}Auth/UserExists?username=${encodeURIComponent(username)}`,
      { withCredentials: true }
    );
    return response.data.exists;
  }

  public async checkPasswordComplexity(password: string): Promise<any> {
    const response = await axios.get(
      `${this.apiUrl}Auth/CheckPasswordComplexity?password=${encodeURIComponent(
        password
      )}`,
      { withCredentials: true }
    );
    return response.data;
  }

  public async getUserProfile(): Promise<any> {
    const response = await axios.get(`${this.apiUrl}AppUser/Me`, {
      withCredentials: true,
    });
    localStorage.setItem('AppUser', JSON.stringify(response.data));
    return response.data;
  }

  public getStoredUserProfile(): any {
    const profile = localStorage.getItem('AppUser');
    return profile ? JSON.parse(profile) : null;
  }

  public isInRole(role: string): boolean {
    const profile = this.getStoredUserProfile();
    return profile?.roles?.includes(role) ?? false;
  }

  private async storeUserProfile(): Promise<void> {
    try {
      await this.getUserProfile();
    } catch (err) {
      console.error('Unable to retrieve user profile:', err);
    }
  }

  private getOrGenerateDeviceId(): string {
    let deviceId = localStorage.getItem('deviceId');
    if (!deviceId) {
      deviceId = this.generateUUID();
      localStorage.setItem('deviceId', deviceId);
    }
    return deviceId;
  }

  private generateUUID(): string {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(
      /[xy]/g,
      function (c) {
        const r = (Math.random() * 16) | 0,
          v = c === 'x' ? r : (r & 0x3) | 0x8;
        return v.toString(16);
      }
    );
  }
}

export default new AuthService();
