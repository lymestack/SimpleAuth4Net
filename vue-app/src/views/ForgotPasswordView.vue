<template>
  <div class="container">
    <h1>Forgot Password</h1>
    <form @submit.prevent="onSubmit">
      <div class="mb-3">
        <label class="form-label">Email</label>
        <input v-model="email" type="email" class="form-control" required />
      </div>
      <button type="submit" class="btn btn-primary">Send Reset Email</button>
    </form>
  </div>
</template>

<script lang="ts">
import { defineComponent } from "vue";
import AuthService from "@/services/AuthService";

export default defineComponent({
  name: "ForgotPasswordView",
  data() {
    return { email: "" };
  },
  methods: {
    async onSubmit() {
      try {
        await AuthService.forgotPassword(this.email);
        alert(
          "Password reset instructions sent. Please check your email for the verification code."
        );
        this.$router.push("/reset-password");
      } catch (e) {
        alert("Error sending reset email.");
      }
    },
  },
});
</script>
