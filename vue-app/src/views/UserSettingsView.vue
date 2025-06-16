<template>
  <div class="container">
    <h1>User Settings</h1>
    <form v-if="user" @submit.prevent="onSubmit">
      <div class="mb-3">
        <label class="form-label">First Name</label>
        <input v-model="user.firstName" class="form-control" />
      </div>
      <div class="mb-3">
        <label class="form-label">Last Name</label>
        <input v-model="user.lastName" class="form-control" />
      </div>
      <button type="submit" class="btn btn-primary">Save</button>
    </form>
  </div>
</template>

<script lang="ts">
import { defineComponent } from "vue";
import AuthService from "@/services/AuthService";

export default defineComponent({
  name: "UserSettingsView",
  data() {
    return { user: null as any };
  },
  async created() {
    try {
      this.user = await AuthService.getUserProfile();
    } catch (e) {
      console.error("Failed to load profile");
    }
  },
  methods: {
    async onSubmit() {
      try {
        await AuthService.updateUserProfile(this.user);
        alert("Profile updated");
      } catch (e) {
        alert("Error updating profile");
      }
    },
  },
});
</script>
