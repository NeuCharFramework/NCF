<template>
  <div class="antv-wrapper">
    <div class="wrapper-canvas" id="wrapper"></div>
  </div>
</template>
<script>
  import { Graph, Shape } from '@antv/x6'
  import { configNodePorts } from '@/utils/antvSetting'
  // 反显数据
  const resData = [{"shape":"edge","attrs":{"line":{"stroke":"#05C13A","strokeWidth":4,"targetMarker":{"name":"block","width":24,"height":16},"strokeDasharray":5,"style":{"animation":"ant-line 30s infinite linear"}}},"id":"390b4bc1-4945-464c-a34d-eeb14acba1a1","zIndex":0,"source":{"cell":"c9ba8f28-9335-447a-a4c0-1dbe34afc815","port":"9daae234-935b-4634-aae4-62fe6e1a763a"},"target":{"cell":"e78740b8-f27f-4f3d-b1d4-11e6e810c76a","port":"122093c7-de48-435b-95aa-8ef9cf58a9ca"},"connector":{"name":"normal"},"vertices":[{"x":640,"y":134}]},{"shape":"edge","attrs":{"line":{"stroke":"#0074FF","targetMarker":{"name":"block","width":12,"height":8},"sourceMarker":{"name":"block","width":12,"height":8}}},"id":"4fa2ff4a-a78e-4f4c-a8fa-0b1c536cbe1b","zIndex":0,"source":{"cell":"e78740b8-f27f-4f3d-b1d4-11e6e810c76a","port":"7ae5d159-3823-4b7f-9e2b-994405071dc8"},"target":{"cell":"9e97d5af-ae22-47e0-bb9f-a0ebf8db0910","port":"40ce97eb-f258-4b8c-b3bc-b798b4662b77"},"vertices":[{"x":1240,"y":250},{"x":1240,"y":400}]},{"shape":"edge","attrs":{"line":{"stroke":"#E36600","strokeWidth":4,"targetMarker":{"name":"block","width":24,"height":16}}},"id":"c0874682-8817-4797-b6e9-5022fd6238b3","zIndex":0,"source":{"cell":"c9ba8f28-9335-447a-a4c0-1dbe34afc815","port":"4b5b6dd0-c31a-42bd-ae81-74b26f67b3eb"},"target":{"cell":"9e97d5af-ae22-47e0-bb9f-a0ebf8db0910","port":"ef42ef10-d01d-49b1-95da-2f19189adf29"},"connector":{"name":"smooth"},"vertices":[{"x":290,"y":290},{"x":380,"y":400}]},{"position":{"x":170,"y":130},"size":{"width":100,"height":50},"attrs":{"text":{"text":"椭圆形"},"body":{"rx":20,"ry":26,"fill":"#08D34F","stroke":"#028222"},"label":{"text":"椭圆形","fontSize":16,"fill":"#FFFFFF"}},"visible":true,"shape":"rect","id":"c9ba8f28-9335-447a-a4c0-1dbe34afc815","data":{"type":"defaultOval"},"ports":{"items":[{"group":"top","id":"e49da871-6793-4a8e-909d-becd630de7cc"},{"group":"right","id":"9daae234-935b-4634-aae4-62fe6e1a763a"},{"group":"bottom","id":"4b5b6dd0-c31a-42bd-ae81-74b26f67b3eb"},{"group":"left","id":"1a7be646-1507-4122-94e0-0ad9d0ab0554"}]},"zIndex":1},{"position":{"x":590,"y":350},"size":{"width":480,"height":100},"attrs":{"text":{"text":"平行四边形"},"body":{"refPoints":"10,0 40,0 30,20 0,20","fill":"#7A0289","stroke":"#49007A"},"label":{"text":"平行四边形","fontSize":"40","fill":"#FFFFFF"}},"visible":true,"shape":"polygon","id":"9e97d5af-ae22-47e0-bb9f-a0ebf8db0910","data":{"type":"defaultRhomboid"},"ports":{"items":[{"group":"top","id":"c6a92550-ceb3-411c-8c4c-7848aa064b76"},{"group":"right","id":"40ce97eb-f258-4b8c-b3bc-b798b4662b77"},{"group":"bottom","id":"a47a0d67-e30b-4149-91b7-527c47043f0a"},{"group":"left","id":"ef42ef10-d01d-49b1-95da-2f19189adf29"}]},"zIndex":2},{"position":{"x":940,"y":10},"size":{"width":80,"height":80},"attrs":{"text":{"text":"圆形"},"body":{"fill":"#CD0000","stroke":"#950000"},"label":{"text":"圆形","fontSize":16,"fill":"#FFFFFF"}},"visible":true,"shape":"circle","id":"e78740b8-f27f-4f3d-b1d4-11e6e810c76a","data":{"type":"defaultCircle"},"ports":{"items":[{"group":"top","id":"11a5b89d-2e19-44db-ad6b-2f03983ea097"},{"group":"right","id":"7ae5d159-3823-4b7f-9e2b-994405071dc8"},{"group":"bottom","id":"358110af-4dd6-4806-8b24-46b2b34b2f7c"},{"group":"left","id":"122093c7-de48-435b-95aa-8ef9cf58a9ca"}]},"zIndex":3}]
  export default {
    name: "jsplumb",
    mounted () {
      this.initGraph()
    },
    methods: {
      // 初始化渲染画布
      initGraph(){
        const graph = new Graph({
          container: document.getElementById('wrapper'),
          grid: true,
          autoResize: true,
          interacting: false,
          connecting: {
            router: {
              name: 'manhattan',
              args: {
                padding: 1,
              },
            },
            connector: {
              name: 'rounded',
              args: {
                radius: 8,
              },
            },
            anchor: 'center',
            connectionPoint: 'anchor',
            allowBlank: false,
            snap: {
              radius: 20,
            },
            createEdge() {
              return new Shape.Edge({
                attrs: {
                  line: {
                    stroke: '#A2B1C3',
                    strokeWidth: 2,
                    targetMarker: {
                      name: 'block',
                      width: 12,
                      height: 8
                    },
                  },
                },
                zIndex: 0,
              })
            }
          },
        })
        // 返现方法
        const portsGroups = configNodePorts().groups
        if(resData.length){
          const jsonTemp = resData.map(item=>{
            if(item.ports) item.ports.groups = portsGroups 
            return item
          })
          graph.fromJSON(jsonTemp)
        }
        graph.centerContent()
      }
    }
  }
</script>
<style lang="scss">
@keyframes ant-line {
  to {
      stroke-dashoffset: -1000
  }
}
</style>
<style lang="scss" scoped="scoped">
  .antv-wrapper{
    position: relative;
    height: 100vh;
    flex: 1;
    .wrapper-canvas{
      height: 100%;
      width: 100%;
      position: relative;
    }
    .wrapper-tips{
      padding: 10px;
      display: flex;
      align-items: center;
      position: absolute;
      top: 0;
      left: 0;
      .wrapper-tips-item{
        span{
          padding-left: 10px;
          font-size: 12px;
        }
      }
    }
  }
</style>
