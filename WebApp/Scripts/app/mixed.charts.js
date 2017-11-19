
var addBarChart = function (action1, action2, formula, chartDiv) {
    setTimeout(function () {
        var updateGraph = function (average, std) {
            var d1 = [];
            var i;
            for (i = 0; i < average[0].valueList.length; i++) {
                d1.push([average[0].valueList[i].groupingVariable[0].item2, average[0].valueList[i].value, std[0].valueList[i].value]);
            };

            var data = [
                {
                    color: "orange",
                    bars: { show: true, align: "center", barWidth: 0.6 },
                    data: $.map(d1, function (itm) { return [[itm[0], itm[1]]]; }),
                    label: "data1"
                },
                {
                    color: "orange",
                    points: { radius: 0, errorbars: "y", yerr: { show: true, upperCap: "-", lowerCap: "-", radius: 5 } },
                    lines: {show: false},
                    data: d1 }
            ];

            $.plot("#" + chartDiv, data,
            {
                xaxis: {
                    mode: "categories",
                    tickLength: 0,
                }/*,
                yaxis: {
                    min: 0,
                    max: 200,
                }*/
            });
        };

        window.mixedModelApp.datacontext.getData(formula, updateGraph, action1, action2);
    }, 1);
};

var addTwoCategoryBarChart = function (action1, action2, formula, chartDiv) {
    setTimeout(function () {
        var updateGraph = function (responseObj, stdObj) {
            var response = responseObj;
            var std = 0;
            if (action2 != 'None') {
                std = stdObj[0];
                response = responseObj[0];
            }

            var d1 = [];
            var subGroupIndices = {};
            var mainGroupIndices = {};
            var mainGroupIndex = 0;
            for (var i = 0; i < response.valueList.length; i++) {

                var subGroupVar = response.valueList[i].groupingVariable[1].item2;
                if (subGroupIndices[subGroupVar] == undefined) {
                    d1.push({
                        label: subGroupVar,
                        data: []
                    });
                    subGroupIndices[subGroupVar] = d1.length - 1;
                };

                var mainGroupVar = response.valueList[i].groupingVariable[0].item2;
                if (mainGroupIndices[mainGroupVar] == undefined) {
                    mainGroupIndices[mainGroupVar] = mainGroupIndex++;
                };

                var stdValue = 0;
                if (action2 != 'None') {
                    stdValue = std.valueList[i].value;
                }
                d1[subGroupIndices[subGroupVar]].data.push([mainGroupIndices[mainGroupVar],
                                                            response.valueList[i].value,
                                                            stdValue]);
            };

            $.plot($("#" + chartDiv), d1,
            {
                series: {
                    points: {
                        //do not show points
                        radius: 0,
                        errorbars: "y",
                        yerr: { show: true, upperCap: "-", lowerCap: "-", radius: 5 }
                    },
                    bars: {
                        show: true,
                        barWidth: 0.13,
                        align: 'center',
                        order: 1
                    }
                },
                xaxis: {
                    mode: null,
                    //min: -0.5,
                    //max: 2.5,
                    ticks: $.map(mainGroupIndices, function (item1, item2) { return [[item1, item2]]; })
                },
                legend: {
                    show: true,
                    container: $('#' + chartDiv + '-aux'),
            },
                bars: {
                    show: true
                }
            });
        };

        window.mixedModelApp.datacontext.getData(formula, updateGraph, action1, action2);
    }, 1);
};

var addLineChart = function (action1, action2, formula, chartDiv) {
    setTimeout(function () {
        var updateGraph = function (responseObj, error) {
            var response;
            if (responseObj.valueList) {
                response = responseObj;
            } else {
                response = responseObj[0];
            }

            var d1 = [];
            var min = response.valueList[0].value;
            var max = response.valueList[0].value;
            for (var i = 0; i < response.valueList.length; i++) {
                d1.push([response.valueList[i].groupingVariable[0].item2, response.valueList[i].value/*, error[0].valueList[i].value*/]);
                min = Math.min(min, response.valueList[i].value);
                max = Math.max(max, response.valueList[i].value);
            };

            var data = [
                //{ color: "orange", bars: { show: true, align: "center", barWidth: 0.25 }, data: d1, label: "data1" },
                {
                    color: "orange",
                    /*points: {
                        //do not show points
                        radius: 0,
                        errorbars: "y",
                        yerr: { show: true, upperCap: "-", lowerCap: "-", radius: 5 }
                    },*/
                    data: d1
                }
            ];

            var plot = $.plot("#" + chartDiv, data);
            var axes = plot.getAxes();
            axes.yaxis.options.min = min - (max - min) * 0.1;
            axes.yaxis.options.max = max + (max - min) * 0.1;
            plot.setupGrid();
            plot.draw();
        };

        window.mixedModelApp.datacontext.getData(formula, updateGraph, action1);
    }, 1);
};

var addMultipleLineChart = function (action1, action2, formula, chartDiv) {
    setTimeout(function () {
        var updateGraph = function (responseObj, stdObj) {
            var response = responseObj;
            var std = 0;
            if (action2 != 'None') {
                std = stdObj[0];
                response = responseObj[0];
            }

            var d1 = [];
            var subGroupIndices = {};
            var mainGroupIndices = {};
            var mainGroupIndex = 0;
            var min = response.valueList[0].value;
            var max = response.valueList[0].value;
            for (var i = 0; i < response.valueList.length; i++) {

                var subGroupVar = response.valueList[i].groupingVariable[1].item2;
                if (subGroupIndices[subGroupVar] == undefined) {
                    d1.push({
                        label: subGroupVar,
                        data: []
                    });
                    subGroupIndices[subGroupVar] = d1.length - 1;
                };

                var mainGroupVar = response.valueList[i].groupingVariable[0].item2;
                if (mainGroupIndices[mainGroupVar] == undefined) {
                    mainGroupIndices[mainGroupVar] = mainGroupIndex++;
                };

                var stdValue = 0;
                if (action2 != 'None') {
                    stdValue = std.valueList[i].value
                }
                d1[subGroupIndices[subGroupVar]].data.push([mainGroupVar,
                                                            response.valueList[i].value,
                                                            stdValue]);
                min = Math.min(min, response.valueList[i].value);
                max = Math.max(max, response.valueList[i].value);
            };

            var plot = $.plot($("#" + chartDiv), d1,
            {
                legend: {
                    show: true,
                    container: $('#' + chartDiv + '-aux'),
                },
                lines: {
                    show: true
                }
            });
            var axes = plot.getAxes();
            axes.yaxis.options.min = min - (max - min) * 0.1;
            axes.yaxis.options.max = max + (max - min) * 0.1;
            plot.setupGrid();
            plot.draw();

        };

        window.mixedModelApp.datacontext.getData(formula, updateGraph, action1, action2);
    }, 1);
};

var addDataTable = function (htmlTable) {
    var data = $.map(window.mixedModelApp.modelViewModel.allVariables(),
                     function(item) {
                         return [[item.modelVariableId,
                                  item.name,
                                  item.valueCount,
                                  item.type,
                                  Math.round(100 * item.average) / 100,
                                  Math.round(100 * item.std) / 100]];
                     });

    $('#' + htmlTable).DataTable({
        data: data
    });
};

var addTransformerTable = function(htmlTable) {
    setTimeout(function () {
        var updateTransformers = function (response) {
            $('#' + htmlTable).DataTable({
                data: response,
                "paging": false,
                "ordering": false,
                "info": false
            });
        };

        window.mixedModelApp.datacontext.getTransformers(updateTransformers);
    }, 1);
};

var addFixedTable = function (htmlTable) {
    setTimeout(function () {
        $('#' + htmlTable).DataTable();
    }, 1);
};

var addLowessChart = function (action1, action2, formula, chartDiv) {
    setTimeout(function () {
        var updateGraph = function (response, lowess) {
            responseObj = response[0];
            var ymin = responseObj.valueList[0].value;
            var ymax = responseObj.valueList[0].value;
            var xmin = responseObj.valueList[0].groupingVariable[0].item2;
            var xmax = responseObj.valueList[0].groupingVariable[0].item2;
            var d1 = [];
            var d2 = [];
            for (var i = 0; i < responseObj.valueList.length; i++) {
                d1.push([responseObj.valueList[i].groupingVariable[0].item2, responseObj.valueList[i].value]);
                d2.push([lowess[0].valueList[i].groupingVariable[0].item2, lowess[0].valueList[i].value]);
                ymin = Math.min(ymin, responseObj.valueList[i].value);
                ymax = Math.max(ymax, responseObj.valueList[i].value);
                xmin = Math.min(xmin, responseObj.valueList[i].groupingVariable[0].item2);
                xmax = Math.max(xmax, responseObj.valueList[i].groupingVariable[0].item2);
            };

            var data = [
                {
                    color: "blue",
                    lines: { show: true },
                    points: { show: false },
                    label: "Loess",
                    data: d2,
                },
                {
                    color: "orange",
                    points: { show: true, },
                    lines: { show: false, },
                    xaxis: {
                        options: {
                            min: -10,
                            max: 10,
                        }
                    },
                    yaxis: {
                        options: {
                            min: -10,
                            max: 10,
                        }
                    },
                    label: "Data",
                    data: d1
                }
            ];

            var plot = $.plot("#" + chartDiv, data, { legend: { position: "se" }, points: { show: true } });
            var axes = plot.getAxes();
            axes.xaxis.options.min = xmin - (xmax - xmin) * 0.1;
            axes.xaxis.options.max = xmax + (xmax - xmin) * 0.1;
            axes.yaxis.options.min = ymin - (ymax - ymin) * 0.1;
            axes.yaxis.options.max = ymax + (ymax - ymin) * 0.1;
            plot.setupGrid();
            plot.draw();
        };

        window.mixedModelApp.datacontext.getData(formula, updateGraph, action1, action2);
    }, 1);
};

var addQQPlot = function (chartDiv) {
    setTimeout(function () {
        var updateGraph = function (response) {
            var d1 = [];
            for (var i = 0; i < response.valueList.length; i++) {
                d1.push([response.valueList[i].groupingVariable[0].item2, response.valueList[i].value]);
            };

            var data = [
                {
                    color: "blue",
                    lines: { show: true },
                    points: {show: false },
                    label: "Theoretic",
                    data: [[-3.2, -3.2], [3.2, 3.2]],
                },
                {
                    color: "red",
                    lines: { show: false },
                    points: { show: false },
                    dashes: { show: true },
                    label: "Upper",
                    data: getQqThresholdData(response.valueList.length, 2),
                },
                {
                    color: "red",
                    lines: { show: false },
                    points: { show: false },
                    dashes: { show: true },
                    label: "Lower",
                    data: getQqThresholdData(response.valueList.length, -2),
                },
                {
                    color: "orange",
                    points: { show: true, },
                    lines: { show: false, },
                    xaxis: {
                        options: {
                            min: -10,
                            max: 10,                            
                        }
                    },
                    yaxis: {
                        options: {
                            min: -10,
                            max: 10,
                        }
                    },
                    label: "Empiric",
                    data: d1
                }
            ];

            var plot = $.plot("#" + chartDiv, data, { legend: {position: "se"}, points: {show: true}});
            var axes = plot.getAxes();
            axes.xaxis.options.min = -3.2;
            axes.xaxis.options.max =  3.2;
            axes.yaxis.options.min = -3.2;
            axes.yaxis.options.max =  3.2;
            plot.setupGrid();
            plot.draw();
        };

        window.mixedModelApp.datacontext.getData('QQ', updateGraph, 'Residuals');
    }, 1);
};

var addFittedPlot = function (chartDiv) {
    setTimeout(function () {
        var updateGraph = function (response) {
            var d1 = [];
            var min = response.valueList[0].groupingVariable[0].item2;
            var max = response.valueList[0].groupingVariable[0].item2;
            for (var i = 0; i < response.valueList.length; i++) {
                d1.push([response.valueList[i].groupingVariable[0].item2, response.valueList[i].value]);
                min = Math.min(min, response.valueList[i].groupingVariable[0].item2);
                max = Math.max(max, response.valueList[i].groupingVariable[0].item2);
            };

            var data = [
                {
                    color: "orange",
                    points: { show: true, },
                    lines: { show: false, },
                    xaxis: {
                        options: {
                            min: -10,
                            max: 10,
                        }
                    },
                    yaxis: {
                        options: {
                            min: -10,
                            max: 10,
                        }
                    },
                    label: "Empiric",
                    data: d1
                }
            ];

            var plot = $.plot("#" + chartDiv, data, { points: { show: true } });
            var axes = plot.getAxes();
            axes.xaxis.options.min = min;
            axes.xaxis.options.max = max;
            axes.yaxis.options.min = -3.5;
            axes.yaxis.options.max = 3.5;
            plot.setupGrid();
            plot.draw();
        };

        window.mixedModelApp.datacontext.getData('Fitted', updateGraph, 'Residuals');
    }, 1);
};
