<template>
  <div class="container">
    <h1>Login</h1>
    <form @submit.prevent="onLogin">
      <div class="mb-3">
        <label for="username" class="form-label">Username</label>
        <input
          v-model="loginModel.username"
          type="text"
          id="username"
          class="form-control"
          required
        />
      </div>
      <div class="mb-3">
        <label for="password" class="form-label">Password</label>
        <input
          v-model="loginModel.password"
          type="password"
          id="password"
          class="form-control"
          required
        />
      </div>
      <div class="mb-3">
        <label class="form-label">MFA Method</label>
        <select v-model.number="mfaMethod" class="form-select">
          <option :value="MfaMethod.Email">Email</option>
          <option :value="MfaMethod.Sms">SMS</option>
          <option :value="MfaMethod.Otp">Authenticator App</option>
        </select>
        <div class="form-check mt-2">
          <input
            class="form-check-input"
            type="checkbox"
            id="rememberChoice"
            v-model="rememberChoice"
          />
          <label class="form-check-label" for="rememberChoice"
            >Remember my choice</label
          >
        </div>
      </div>
      <button type="submit" class="btn btn-primary">Login</button>
    </form>
    <p class="mt-3">
      <router-link to="/forgot-password">Forgot password?</router-link>
      |
      <router-link to="/register">Register</router-link>
    </p>
  </div>
</template>

<script lang="ts">
import { defineComponent } from "vue";
import AuthService, { LoginModel, MfaMethod } from "@/services/AuthService";

export default defineComponent({
  name: "LoginView",
  data() {
    return {
      loginModel: {
        username: "",
        password: "",
        deviceId: AuthService["deviceId"],
      } as LoginModel,
      mfaMethod: (localStorage.getItem("preferredMfaMethod")
        ? parseInt(localStorage.getItem("preferredMfaMethod") as string, 10)
        : MfaMethod.Email) as MfaMethod,
      rememberChoice: true,
      MfaMethod,
    };
  },
  methods: {
    async onLogin() {
      try {
        this.loginModel.mfaMethod = this.mfaMethod;
        const result = await AuthService.login(this.loginModel);
        if (this.rememberChoice) {
          localStorage.setItem("preferredMfaMethod", this.mfaMethod.toString());
        }
        if (this.mfaMethod === MfaMethod.Email) {
          this.$router.push("/verify-mfa-email");
        } else if (this.mfaMethod === MfaMethod.Sms) {
          this.$router.push("/verify-mfa-sms");
        } else if (this.mfaMethod === MfaMethod.Otp) {
          this.$router.push("/verify-mfa-otp");
        } else {
          this.$router.push("/");
        }
      } catch (error) {
        alert("Login failed. Please try again.");
      }
    },
  },
});
</script>
