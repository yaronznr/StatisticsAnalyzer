window.mixedModelApp = window.mixedModelApp || {};

window.mixedModelApp.datacontext = (function () {

    var self = this;
    self.duringAnalysis = false;

    var datacontext = {
        getModel: getModel,
        saveModel: saveModel,
        getModelAnalysis: getModelAnalysis,
        getData: getData,
        getSamples: getSamples,
        setSample: setSample,
        addTransformer: addTransformer,
        removeTransformer: removeTransformer,
        getTransformers: getTransformers,
        ajaxRequest: ajaxRequest,
        explainQQ: explainQQ,
        explainFittedResid: explainFittedResid
    };

    return datacontext;

    function getModel(viewModel) {
        clearErrorMessage(viewModel.errorMessage);
        return ajaxRequest("get", modelUrl())
            .done(function (data) {
                viewModel.allVariables(data.variables);
                viewModel.rowCount(data.rowCount);
                viewModel.tableAnalysis(data.tableAnalysis);
                viewModel.formula(data.formula);
                viewModel.interpert(data.modelInterpert);
                viewModel.intent(data.modelIntent);
                viewModel.resetQuestions(data.modelInterpert, data.modelIntent, data.data);
                if (data.modelAnalysis) {
                    viewModel.clearQuestionsExceptFirst();
                    viewModel.addQuestions(data.modelAnalysis.questions);
                };
                viewModel.inProgress(false);
            })
            .fail(function () {
                viewModel.errorMessage("Error retrieving statistical model.");
            });
    }

    function saveModel(viewModel) {
        clearErrorMessage(viewModel.errorMessage);
        return ajaxRequest("post", modelUrl(), viewModel.formula())
            .done(function (result) {
                if (!self.duringAnalysis) {
                    viewModel.inProgress(false);
                    viewModel.allVariables(result.variables);
                    viewModel.rowCount(result.rowCount);
                    viewModel.tableAnalysis(result.tableAnalysis);
                    viewModel.interpert(result.modelInterpert);
                    viewModel.resetQuestions(result.modelInterpert, result.modelIntent, result.data);
                }
            })
            .fail(function () {
                viewModel.errorMessage("Error saving model analysis");
                viewModel.inProgress(false);
            });
    }
    function getModelAnalysis(viewModel) {
        clearErrorMessage(viewModel.errorMessage);
        viewModel.clearQuestionsExceptFirst();
        self.duringAnalysis = true;
        return ajaxRequest("post", modelAnalysisUrl(), viewModel.formula())
            .done(function (data) {
                self.duringAnalysis = false;
                viewModel.addQuestions(data.questions);
                viewModel.inProgress(false);
            })
            .fail(function () {
                self.duringAnalysis = false;
                viewModel.errorMessage("Error saving model analysis");
                viewModel.inProgress(false);
            });
    }
    function getData(formula, updateGraph, action1, action2) {
        if (action2 && action2 != "None") {
            return getData2(formula, updateGraph, action1, action2);
        };

        return ajaxRequest("post", modelDataUrl(action1), formula)
            .done(function (data) {
                updateGraph(data);
            });
    }
    function getData2(formula, updateGraph, action1, action2) {
        return $.when(ajaxRequest("post", modelDataUrl(action1), formula), 
                      ajaxRequest("post", modelDataUrl(action2), formula))
                .done(function(data1, data2) {
                           updateGraph(data1, data2);
                      });
    }
    function explainQQ() {
        window.open("Pages/explainQQ.html");
    }
    function explainFittedResid() {
        window.open("Pages/explainFittedResid.html");
    }
    function addTransformer(transformer) {
        alert("Data row removed. Analysis will refresh in next run");
        return ajaxRequest("post", modelTransformerUrl('addtransformer'), transformer);
    }
    function removeTransformer(transformer) {
        return ajaxRequest("delete", modelTransformerUrl('removetransformer'), transformer);
    }
    function getTransformers(updateTransformers) {
        return ajaxRequest("get", modelTransformerUrl())
            .done(function (data) {
                updateTransformers(data);
            });
    }

    function getSamples(viewModel) {
        return ajaxRequest("get", getSamplesUrl())
            .done(function (data) {
                viewModel.samples(data.samples);
        });
    }

    function setSample(sample, viewModel) {
        return ajaxRequest("post", setSampleUrl(), sample)
            .done(function () {
            });
    }

    // Private
    function clearErrorMessage(errorMessage) {
        errorMessage(null);
    }

    function ajaxRequest(type, url, data, dataType) { // Ajax helper
        var options = {
            dataType: dataType || "json",
            contentType: "application/json",
            cache: false,
            type: type,
            data: data
        };
        var antiForgeryToken = $("#antiForgeryToken").val();
        if (antiForgeryToken) {
            options.headers = {
                'RequestVerificationToken': antiForgeryToken
            };
        }
        return $.ajax(url, options);
    }

    // routes
    function modelUrl(id) { return "/api/model/" + (id || ""); }
    function modelAnalysisUrl(id) { return "/api/analysis/" + (id || ""); }
    function modelDataUrl(action, id) { return "/api/data/" + action + "/" + (id || ""); }
    function modelTransformerUrl(action, id) { return "/api/transformer/" + action + (id || ""); }
    function getSamplesUrl() { return "/api/samples/samples/"; }
    function setSampleUrl() { return "/api/samples/"; }

})();