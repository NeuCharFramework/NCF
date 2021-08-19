<template>
  <div class="about">
    <h1>11111111111111111111关于页面11111111111</h1>
    <h1>11111111111111111111关于页面11111111111</h1>
  </div>
</template>
<script>
export default {
  name: 'about',
  mounted() {
    setTimeout(() => {
      this.$eventBus.emit('visitedAbout')
    }, 500)
  }
}
</script>

