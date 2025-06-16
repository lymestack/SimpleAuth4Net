<template>
  <div class="container">
    <h1>Verify Account</h1>
    <form @submit.prevent="onSubmit">
      <div class="mb-3">
        <label class="form-label">Verification Code</label>
        <input v-model="code" class="form-control" required />
      </div>
      <button type="submit" class="btn btn-primary">Verify</button>
    </form>
  </div>
</template>

<script lang="ts">
import { defineComponent } from "vue";
import AuthService from "@/services/AuthService";

export default defineComponent({
  name: "VerifyAccountView",
  data() {
    return { code: "" };
  },
  methods: {
    async onSubmit() {
      try {
        await AuthService.verifyAccount(this.code);
        alert("Account verified.");
        this.$router.push("/login");
      } catch (e) {
        alert("Verification failed.");
      }
    },
  },
});
</script>
