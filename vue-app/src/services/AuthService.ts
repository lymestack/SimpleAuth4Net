import axios from "axios";

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
  private baseUrl: string = "http://localhost/SimpleAuthNet/api/"; // <-- Use this for IIS
  // private baseUrl: string = "http://localhost:5218/";
  private apiUrl: string = `${this.baseUrl}Auth/`;
  private deviceId: string = this.getOrGenerateDeviceId();

  // Store token expiration in local storage
  private storeTokenExpiration(
    accessTokenExpires: string,
    refreshTokenExpires?: string
  ): void {
    const accessExpirationTime = Date.parse(accessTokenExpires);
    if (!isNaN(accessExpirationTime)) {
      localStorage.setItem(
        "accessTokenExpiration",
        accessExpirationTime.toString()
      );
    }
    if (refreshTokenExpires) {
      const refreshExpirationTime = Date.parse(refreshTokenExpires);
      if (!isNaN(refreshExpirationTime)) {
        localStorage.setItem(
          "refreshTokenExpiration",
          refreshExpirationTime.toString()
        );
      }
    }
  }

  private getStoredTokenExpiration(): number | null {
    const expiration = localStorage.getItem("accessTokenExpiration");
    return expiration ? parseInt(expiration, 10) : null;
  }

  private getStoredRefreshTokenExpiration(): number | null {
    const expiration = localStorage.getItem("refreshTokenExpiration");
    return expiration ? parseInt(expiration, 10) : null;
  }

  public async login(loginModel: LoginModel): Promise<any> {
    try {
      const response = await axios.post(`${this.apiUrl}Login`, loginModel, {
        withCredentials: true,
      });
      this.storeTokenExpiration(
        response.data.expires,
        response.data.refreshTokenExpires
      );
      return response.data;
    } catch (error) {
      console.error("Error during login:", error);
      throw error;
    }
  }

  public async refreshToken(): Promise<any> {
    try {
      const response = await axios.get(
        `${this.apiUrl}RefreshToken?deviceId=${this.deviceId}`,
        {
          withCredentials: true,
        }
      );
      this.storeTokenExpiration(
        response.data.expires,
        response.data.refreshTokenExpires
      );
      return response.data;
    } catch (error) {
      console.error("Error refreshing token:", error);
      throw error;
    }
  }

  public async logout(): Promise<void> {
    try {
      await axios.delete(`${this.apiUrl}Logout`, { withCredentials: true });
      localStorage.removeItem("accessTokenExpiration");
      localStorage.removeItem("refreshTokenExpiration");
      localStorage.removeItem("deviceId");
      alert("Logged out successfully.");
    } catch (error) {
      console.error("Error during logout:", error);
      alert("Error logging out. Please try again.");
    }
  }

  public async register(model: RegisterModel): Promise<any> {
    const response = await axios.post(`${this.apiUrl}Register`, model, {
      withCredentials: true,
    });
    return response.data;
  }

  public async forgotPassword(email: string): Promise<any> {
    const response = await axios.post(
      `${this.apiUrl}ForgotPassword`,
      { email },
      { withCredentials: true }
    );
    return response.data;
  }

  public async resetPassword(
    username: string,
    newPassword: string,
    verifyToken: string
  ): Promise<any> {
    const response = await axios.post(
      `${this.apiUrl}ResetPassword`,
      { username, newPassword, verifyToken },
      { withCredentials: true }
    );
    return response.data;
  }

  public async verifyAccount(code: string): Promise<any> {
    const username = localStorage.getItem("verifyUsername");
    const response = await axios.post(
      `${this.apiUrl}VerifyAccount`,
      { username, verifyToken: code, deviceId: this.deviceId },
      { withCredentials: true }
    );
    return response.data;
  }

  public async getUserProfile(): Promise<any> {
    const response = await axios.get(`${this.baseUrl}AppUser/Me`, {
      withCredentials: true,
    });
    return response.data;
  }

  public async updateUserProfile(user: any): Promise<any> {
    const response = await axios.post(`${this.baseUrl}AppUser`, user, {
      withCredentials: true,
    });
    return response.data;
  }

  public isLoggedIn(): boolean {
    const now = new Date().getTime();
    const accessTokenExpires = this.getStoredTokenExpiration();
    const refreshTokenExpires = this.getStoredRefreshTokenExpiration();

    if (accessTokenExpires && now < accessTokenExpires) {
      return true;
    }
    if (refreshTokenExpires && now < refreshTokenExpires) {
      console.log("Access token expired but refresh token is valid.");
      return true;
    }
    return false;
  }

  private getOrGenerateDeviceId(): string {
    let deviceId = localStorage.getItem("deviceId");
    if (!deviceId) {
      deviceId = this.generateUUID();
      localStorage.setItem("deviceId", deviceId);
    }
    return deviceId;
  }

  private generateUUID(): string {
    return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(
      /[xy]/g,
      function (c) {
        const r = (Math.random() * 16) | 0,
          v = c === "x" ? r : (r & 0x3) | 0x8;
        return v.toString(16);
      }
    );
  }
}

export default new AuthService();
