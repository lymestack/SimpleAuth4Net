<template>
  <div class="container">
    <h1>Reset Password</h1>
    <form @submit.prevent="onSubmit">
      <div class="mb-3">
        <label class="form-label">Username</label>
        <input v-model="username" class="form-control" required />
      </div>
      <div class="mb-3">
        <label class="form-label">Verification Code</label>
        <input v-model="code" class="form-control" required />
      </div>
      <div class="mb-3">
        <label class="form-label">New Password</label>
        <input
          v-model="newPassword"
          type="password"
          class="form-control"
          required
        />
      </div>
      <button type="submit" class="btn btn-primary">Reset Password</button>
    </form>
  </div>
</template>

<script lang="ts">
import { defineComponent } from "vue";
import AuthService from "@/services/AuthService";

export default defineComponent({
  name: "ResetPasswordView",
  data() {
    return {
      username: "",
      code: "",
      newPassword: "",
    };
  },
  methods: {
    async onSubmit() {
      try {
        await AuthService.resetPassword(
          this.username,
          this.newPassword,
          this.code
        );
        alert("Password reset successful.");
        this.$router.push("/login");
      } catch (e) {
        alert("Password reset failed.");
      }
    },
  },
});
</script>
