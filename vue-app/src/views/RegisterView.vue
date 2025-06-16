<template>
  <div class="container">
    <h1>Register</h1>
    <form @submit.prevent="onRegister">
      <div class="mb-3">
        <label class="form-label">First Name</label>
        <input v-model="model.firstName" class="form-control" required />
      </div>
      <div class="mb-3">
        <label class="form-label">Last Name</label>
        <input v-model="model.lastName" class="form-control" required />
      </div>
      <div class="mb-3">
        <label class="form-label">Email</label>
        <input v-model="model.emailAddress" type="email" class="form-control" required />
      </div>
      <div class="mb-3">
        <label class="form-label">Username</label>
        <input v-model="model.username" class="form-control" required />
      </div>
      <div class="mb-3">
        <label class="form-label">Password</label>
        <input v-model="model.password" type="password" class="form-control" required />
      </div>
      <div class="mb-3">
        <label class="form-label">Confirm Password</label>
        <input v-model="model.confirmPassword" type="password" class="form-control" required />
      </div>
      <button type="submit" class="btn btn-primary">Register</button>
    </form>
  </div>
</template>

<script lang="ts">
import { defineComponent } from "vue";
import AuthService, { RegisterModel } from "@/services/AuthService";

export default defineComponent({
  name: "RegisterView",
  data() {
    return {
      model: {
        firstName: "",
        lastName: "",
        emailAddress: "",
        username: "",
        password: "",
        confirmPassword: "",
      } as RegisterModel,
    };
  },
  methods: {
    async onRegister() {
      try {
        await AuthService.register(this.model);
        alert("Registration successful. Please verify your account.");
        this.$router.push("/login");
      } catch (err) {
        alert("Registration failed.");
      }
    },
  },
});
</script>
