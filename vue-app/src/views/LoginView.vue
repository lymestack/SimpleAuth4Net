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
import AuthService, { LoginModel } from "@/services/AuthService";

export default defineComponent({
  name: "LoginView",
  data() {
    return {
      loginModel: {
        username: "",
        password: "",
        deviceId: AuthService["deviceId"],
      } as LoginModel,
    };
  },
  methods: {
    async onLogin() {
      try {
        const result = await AuthService.login(this.loginModel);
        alert(`Login successful! Welcome, ${result.username}.`);
        this.$router.push("/");
      } catch (error) {
        alert("Login failed. Please try again.");
      }
    },
  },
});
</script>
