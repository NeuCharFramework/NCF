<template>
  <div class="hello">
    <h1>{{ msg }}</h1>
    <img src="../assets/logo.png" style="width: 100px;height: 100px;" height="200" width="200"/>
  </div>
</template>

<script>
export default {
  name: 'HelloWorld',
  props: {
    msg: String
  }
}
</script>
