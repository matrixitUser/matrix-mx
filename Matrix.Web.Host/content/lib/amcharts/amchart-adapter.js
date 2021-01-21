function amAdapt(div, data) {

    var chart = AmCharts.makeChart(div, data);

    chart.addListener("dataUpdated", zoomChart);
    zoomChart();

    function zoomChart() {
        chart.zoomToIndexes(chart.dataProvider.length - 20, chart.dataProvider.length - 1);
    }
}