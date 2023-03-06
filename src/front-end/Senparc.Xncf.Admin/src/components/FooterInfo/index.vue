<template>
  <div class="footerInfo">
    <span>© 2022 -</span>
    <a href="http://www.senparc.com/">苏州盛派网络科技有限公司</a>
  </div>
</template>

<script>
export default {
  name: "footerinfo",
};
</script>

<style scoped lang="scss">
.footerInfo {
  display: flex;
  align-items: center;
  font-family: "Source Sans Pro", sans-serif;
  font-size: 14px;
  padding: 40px 20px;
  border-top: 1px solid #e4eaec;

  span {
    color: #656565;
  }
}
</style>