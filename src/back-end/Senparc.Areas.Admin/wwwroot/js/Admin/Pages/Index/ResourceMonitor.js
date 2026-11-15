/*----------------------------------------------------------------
 * ResourceMonitor.js
 * Admin 首页 Host 实时与历史资源监控图表。
 * 后台记录保留在 CO2NET 缓存中，页面内的 2 秒采样仅保留最近 5 分钟。
 *
 * 修改标识：Senparc - 20261102
 * 修改描述：v0.12.7 合并实时和历史曲线，使用线性时间轴与范围缩放
 *----------------------------------------------------------------*/
(function (global) {
  'use strict';

  var FULL_SPAN_MS = 24 * 60 * 60 * 1000;
  var LIVE_SPAN_MS = 5 * 60 * 1000;
  var DEFAULT_SPAN_MS = 30 * 60 * 1000;
  var MIN_ZOOM_MS = 30 * 1000;
  var GRID = { left: 52, right: 58, top: 72, bottom: 42 };
  var FIELDS = ['cpu', 'pcpu', 'mem', 'pmem', 'rx', 'tx'];
  var COLORS = ['#8c52ff', '#00a6a6', '#67c23a', '#f56c6c', '#409eff', '#e6a23c'];

  function validNumber(value) {
    return typeof value === 'number' && Number.isFinite(value);
  }

  function pad2(value) {
    return value < 10 ? '0' + value : '' + value;
  }

  function formatTime(timestamp, withSeconds, crossDay) {
    var date = new Date(timestamp);
    if (Number.isNaN(date.getTime())) {
      return '--';
    }
    var time = pad2(date.getHours()) + ':' + pad2(date.getMinutes()) +
      (withSeconds ? ':' + pad2(date.getSeconds()) : '');
    return crossDay ? pad2(date.getMonth() + 1) + '-' + pad2(date.getDate()) + ' ' + time : time;
  }

  function mergePoints(windows, livePoints) {
    var points = [];
    var covered = { 10: new Set(), 60: new Set(), 300: new Set() };

    function add(point, resolution) {
      if (!point || !validNumber(point.t)) {
        return;
      }
      var ownBuckets = covered[resolution];
      if (ownBuckets && ownBuckets.has(Math.floor(point.t / (resolution * 1000)))) {
        return;
      }
      points.push(Object.assign({}, point, { resolutionSeconds: resolution }));
      [10, 60, 300].forEach(function (seconds) {
        if (resolution <= seconds) {
          covered[seconds].add(Math.floor(point.t / (seconds * 1000)));
        }
      });
    }

    livePoints.forEach(function (point) { add(point, 2); });
    (windows || []).slice().sort(function (a, b) {
      return a.resolutionSeconds - b.resolutionSeconds;
    }).forEach(function (window) {
      (window.points || []).forEach(function (point) {
        add(point, window.resolutionSeconds);
      });
    });
    return points.sort(function (a, b) { return a.t - b.t; });
  }

  function markGaps(points) {
    var result = [];
    points.forEach(function (point, index) {
      if (index > 0) {
        var previous = points[index - 1];
        var gap = point.t - previous.t;
        var expected = Math.max(previous.resolutionSeconds, point.resolutionSeconds) * 1000;
        if (gap > expected * 2.5) {
          result.push({ t: previous.t + gap / 2 });
        }
      }
      result.push(point);
    });
    return result;
  }

  function create(el, options) {
    options = options || {};
    var i18n = options.i18n || {};
    var selectedRange = options.rangeMs || DEFAULT_SPAN_MS;
    var chart = echarts.init(el);
    var overlay = document.createElement('div');
    var emptyEl = document.createElement('div');
    var history = [];
    var livePoints = [];
    var latestTime = 0;
    var windowStart = 0;
    var windowEnd = 0;
    var live = true;
    var dragging = false;
    var dragStartX = 0;
    var selectedSeries = {};
    var disposed = false;

    emptyEl.className = 'rm-empty';
    emptyEl.textContent = i18n.empty || '';
    emptyEl.style.display = 'none';
    el.appendChild(emptyEl);
    overlay.className = 'rm-select-overlay';
    overlay.style.display = 'none';
    el.appendChild(overlay);

    chart.on('legendselectchanged', function (event) {
      selectedSeries = Object.assign({}, event.selected);
    });

    function now() {
      return latestTime || Date.now();
    }

    function reportView() {
      if (typeof options.onViewChange === 'function') {
        options.onViewChange({
          start: windowStart,
          end: windowEnd,
          live: live,
          zoomed: Math.abs(windowEnd - windowStart - selectedRange) > 1
        });
      }
    }

    function setWindow(start, end, isLive) {
      var newest = now();
      var span = Math.min(FULL_SPAN_MS, Math.max(MIN_ZOOM_MS, end - start));
      end = Math.min(newest, Math.max(newest - FULL_SPAN_MS + span, end));
      windowStart = end - span;
      windowEnd = end;
      live = isLive;
      reportView();
    }

    function followLatest() {
      if (!windowEnd || live) {
        setWindow(now() - selectedRange, now(), true);
      }
    }

    function makeSeries(points, field, name, color, dashed, network) {
      var validCount = 0;
      var data = points.map(function (point) {
        var value = validNumber(point[field]) ? point[field] : '-';
        if (value !== '-') {
          validCount++;
        }
        return [point.t, value];
      });
      return {
        name: name,
        type: 'line',
        yAxisIndex: network ? 1 : 0,
        showSymbol: validCount === 1,
        symbolSize: 6,
        smooth: false,
        connectNulls: false,
        data: data,
        lineStyle: { normal: { color: color, width: 1.6, type: dashed ? 'dashed' : 'solid' } },
        itemStyle: { normal: { color: color } }
      };
    }

    function render() {
      if (disposed || !windowEnd) {
        return;
      }
      var span = windowEnd - windowStart;
      var withSeconds = span < 90 * 60 * 1000;
      var startDate = new Date(windowStart);
      var endDate = new Date(windowEnd);
      var crossDay = startDate.toDateString() !== endDate.toDateString();
      var points = mergePoints(history, livePoints).filter(function (point) {
        return point.t >= windowStart && point.t <= windowEnd;
      });
      var hasValues = points.some(function (point) {
        return FIELDS.some(function (field) { return validNumber(point[field]); });
      });

      points = markGaps(points);
      chart.setOption({
        animation: false,
        grid: { left: GRID.left, right: GRID.right, top: GRID.top, bottom: GRID.bottom },
        tooltip: {
          trigger: 'axis',
          axisPointer: { type: 'line', lineStyle: { color: '#8c52ff', opacity: 0.5 } },
          formatter: function (params) {
            if (!params || !params.length) {
              return '';
            }
            var first = params[0].value;
            var timestamp = Array.isArray(first) ? first[0] : params[0].name;
            var lines = [formatTime(timestamp, true, crossDay)];
            params.forEach(function (param) {
              var value = Array.isArray(param.value) ? param.value[1] : param.value;
              if (!validNumber(value)) {
                return;
              }
              var unit = param.seriesIndex >= 4 ? ' Mbps' : '%';
              var color = COLORS[param.seriesIndex] || '#909399';
              var marker = '<span style="display:inline-block;width:9px;height:9px;' +
                'border-radius:50%;background:' + color + ';margin-right:5px"></span>';
              lines.push(marker + param.seriesName + ': ' + value.toFixed(1) + unit);
            });
            return lines.join('<br>');
          }
        },
        legend: {
          top: 2,
          left: 'center',
          selected: selectedSeries,
          data: [i18n.cpuHost, i18n.cpuWeb, i18n.memHost, i18n.memWeb, i18n.rx, i18n.tx]
        },
        xAxis: {
          type: 'time',
          min: windowStart,
          max: windowEnd,
          boundaryGap: false,
          axisLine: { lineStyle: { color: '#dcdfe6' } },
          axisTick: { show: false },
          splitLine: { show: false },
          axisLabel: {
            color: '#909399',
            formatter: function (value) { return formatTime(value, withSeconds, crossDay); }
          }
        },
        yAxis: [{
          type: 'value',
          min: 0,
          max: 100,
          name: '%',
          nameTextStyle: { color: '#909399' },
          axisLabel: { color: '#909399' },
          splitLine: { lineStyle: { color: '#f0f2f5' } }
        }, {
          type: 'value',
          min: 0,
          name: 'Mbps',
          nameTextStyle: { color: '#909399' },
          axisLabel: { color: '#909399' },
          splitLine: { show: false }
        }],
        series: [
          makeSeries(points, 'cpu', i18n.cpuHost, COLORS[0], false, false),
          makeSeries(points, 'pcpu', i18n.cpuWeb, COLORS[1], true, false),
          makeSeries(points, 'mem', i18n.memHost, COLORS[2], false, false),
          makeSeries(points, 'pmem', i18n.memWeb, COLORS[3], true, false),
          makeSeries(points, 'rx', i18n.rx, COLORS[4], false, true),
          makeSeries(points, 'tx', i18n.tx, COLORS[5], false, true)
        ]
      }, true);
      emptyEl.style.display = hasValues ? 'none' : 'flex';
    }

    function update(payload) {
      if (disposed) {
        return;
      }
      history = payload.windows;
      latestTime = Math.max(latestTime, payload.now);
      followLatest();
      render();
    }

    function addLiveSample(point) {
      if (disposed) {
        return;
      }
      latestTime = Math.max(latestTime, point.t);
      livePoints.push(point);
      livePoints = livePoints.filter(function (sample) {
        return sample.t >= latestTime - LIVE_SPAN_MS;
      }).slice(-151);
      followLatest();
      if (live || (point.t >= windowStart && point.t <= windowEnd)) {
        render();
      }
    }

    function setRange(rangeMs) {
      if (!validNumber(rangeMs) || rangeMs < MIN_ZOOM_MS || rangeMs > FULL_SPAN_MS) {
        throw new RangeError('Resource monitor range is invalid.');
      }
      selectedRange = rangeMs;
      setWindow(now() - rangeMs, now(), true);
      render();
    }

    function reset() {
      setWindow((windowEnd || now()) - selectedRange, windowEnd || now(), live);
      render();
    }

    function returnToLive() {
      setWindow(now() - selectedRange, now(), true);
      render();
    }

    function localPosition(event) {
      var rect = el.getBoundingClientRect();
      return { x: event.clientX - rect.left, y: event.clientY - rect.top };
    }

    function inPlot(position) {
      return position.x >= GRID.left && position.x <= el.clientWidth - GRID.right &&
        position.y >= GRID.top && position.y <= el.clientHeight - GRID.bottom;
    }

    function boundedX(x) {
      return Math.max(GRID.left, Math.min(el.clientWidth - GRID.right, x));
    }

    function timeAtX(x) {
      var width = el.clientWidth - GRID.left - GRID.right;
      return windowStart + (windowEnd - windowStart) * (boundedX(x) - GRID.left) / width;
    }

    function onMouseDown(event) {
      var position = localPosition(event);
      if (event.button !== 0 || disposed || !windowEnd || !inPlot(position)) {
        return;
      }
      dragging = true;
      dragStartX = position.x;
      el.style.cursor = 'col-resize';
      event.preventDefault();
    }

    function onMouseMove(event) {
      if (!dragging) {
        return;
      }
      var x = boundedX(localPosition(event).x);
      overlay.style.display = 'block';
      overlay.style.left = Math.min(dragStartX, x) + 'px';
      overlay.style.top = GRID.top + 'px';
      overlay.style.width = Math.abs(x - dragStartX) + 'px';
      overlay.style.height = Math.max(0, el.clientHeight - GRID.top - GRID.bottom) + 'px';
    }

    function onMouseUp(event) {
      if (!dragging) {
        return;
      }
      dragging = false;
      el.style.cursor = '';
      overlay.style.display = 'none';
      var x = boundedX(localPosition(event).x);
      if (disposed || Math.abs(x - dragStartX) < 3) {
        return;
      }
      var start = timeAtX(Math.min(dragStartX, x));
      var end = timeAtX(Math.max(dragStartX, x));
      if (end - start >= MIN_ZOOM_MS) {
        setWindow(start, end, false);
        render();
      }
    }

    function onWheel(event) {
      var position = localPosition(event);
      if (disposed || !windowEnd || !event.deltaY || !inPlot(position)) {
        return;
      }
      event.preventDefault();
      var span = windowEnd - windowStart;
      var newSpan = Math.min(FULL_SPAN_MS, Math.max(MIN_ZOOM_MS,
        span * (event.deltaY < 0 ? 0.8 : 1.25)));
      var ratio = (position.x - GRID.left) / (el.clientWidth - GRID.left - GRID.right);
      var anchor = windowStart + span * ratio;
      setWindow(anchor - newSpan * ratio, anchor + newSpan * (1 - ratio), false);
      render();
    }

    function onDoubleClick(event) {
      if (inPlot(localPosition(event))) {
        reset();
      }
    }

    function onResize() {
      if (!disposed) {
        chart.resize();
      }
    }

    var resizeObserver = typeof global.ResizeObserver === 'function'
      ? new global.ResizeObserver(onResize)
      : null;
    if (resizeObserver) {
      resizeObserver.observe(el);
    }

    el.addEventListener('mousedown', onMouseDown);
    el.addEventListener('mousemove', onMouseMove);
    el.addEventListener('wheel', onWheel, { passive: false });
    el.addEventListener('dblclick', onDoubleClick);
    global.addEventListener('mouseup', onMouseUp);
    global.addEventListener('resize', onResize);

    return {
      update: update,
      addLiveSample: addLiveSample,
      setRange: setRange,
      reset: reset,
      returnToLive: returnToLive,
      dispose: function () {
        disposed = true;
        global.removeEventListener('mouseup', onMouseUp);
        global.removeEventListener('resize', onResize);
        if (resizeObserver) {
          resizeObserver.disconnect();
        }
        el.removeEventListener('mousedown', onMouseDown);
        el.removeEventListener('mousemove', onMouseMove);
        el.removeEventListener('wheel', onWheel);
        el.removeEventListener('dblclick', onDoubleClick);
        chart.dispose();
        if (overlay.parentNode) {
          overlay.parentNode.removeChild(overlay);
        }
        if (emptyEl.parentNode) {
          emptyEl.parentNode.removeChild(emptyEl);
        }
      }
    };
  }

  global.ResourceMonitorChart = { create: create };
})(window);
