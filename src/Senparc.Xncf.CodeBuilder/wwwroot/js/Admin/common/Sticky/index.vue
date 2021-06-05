<template>
  <div :style="{height:height+'px',zIndex:zIndex}">
    <div :class="className" class="sticky" :style="{top:stickyTop+'px',zIndex:zIndex,position:position,width:width,height:height+'px'}">
      <slot>
        <div>sticky</div>
      </slot>
    </div>
  </div>
</template>

<script>
export default {
  name: 'Sticky',
  props: {
    stickyTop: {
      type: Number,
      default: 44
    },
    zIndex: {
      type: Number,
      default: 200
    }
  },
  data() {
    return {
      active: false,
      position: '',
      width: undefined,
      height: undefined,
      isSticky: false,
      className: ''
    }
  },
  mounted() {
    this.height = this.$el.getBoundingClientRect().height
    window.addEventListener('scroll', this.handleScroll)
    window.addEventListener('resize', this.handleReize)
  },
  activated() {
    this.handleScroll()
  },
  destroyed() {
    window.removeEventListener('scroll', this.handleScroll)
    window.removeEventListener('resize', this.handleReize)
  },
  methods: {
    sticky() {
      if (this.active) {
        return
      }
      this.position = 'fixed'
      this.active = true
      this.width = this.width + 'px'
      this.isSticky = true
    },
    reset() {
      if (!this.active) {
        return
      }
      this.position = ''
      this.width = 'auto'
      this.active = false
      this.isSticky = false
    },
    handleScroll() {
      this.width = this.$el.getBoundingClientRect().width
      if (this.width === 0) {
        this.width = '100%'
      }
      const offsetTop = this.$el.getBoundingClientRect().top
      if (offsetTop < this.stickyTop) {
        this.className = 'box-shadow'
        this.sticky()
        return
      }
      this.reset()
    },
    handleReize() {
      if (this.isSticky) {
        this.width = this.$el.getBoundingClientRect().width + 'px'
      }
    }
  }
}
</script>
<style scoped>
.box-shadow {
    border-bottom-color: #e9e9e9;
    box-shadow: 0 0 3px rgba(0, 0, 0, 0.1);
}
.sticky {
    /* background-color: rgb(48, 65, 86); */
    background-color: #2d3a4b;
    background-color: #24262f;
    background-color: #f8f8f8;
    text-align: right;
    line-height: 42px;
    padding-right: 20px;
}
</style>
