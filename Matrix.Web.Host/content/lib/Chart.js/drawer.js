function drawG(id,legend, data) {
    var canvas = document.getElementById(id);
    var ctx = canvas.getContext("2d");
    window.myLine = new Chart(ctx).Line(data, {
        responsive: true,
        bezierCurve: true,
        scaleUse2Y:true,
        legendTemplate: "<ul class=\"<%=name.toLowerCase()%>-legend\"><% for (var i=0; i<datasets.length; i++){%><li><span style=\"background-color:<%if(datasets[i].strokeColor){%><%=datasets[i].strokeColor%><%}%>\"><%if(datasets[i].label){%><%=datasets[i].label%><%}%></span></li><%}%></ul>"
        //legendTemplate: "<ul class=\"<%=name.toLowerCase()%>-legend\"><% for (var i=0; i<segments.length; i++){%><li><span style=\"-moz-border-radius:7px 7px 7px 7px; border-radius:7px 7px 7px 7px; margin-right:10px;width:15px;height:15px;display:inline-block;background-color:<%=segments[i].fillColor%>\"></span><%if(segments[i].label){%><%=segments[i].label%><%}%></li><%}%></ul>"
    });

    document.getElementById(legend).innerHTML = window.myLine.generateLegend();
};