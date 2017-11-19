
var applyStaticDragObjects = function () {
    $('.variableArea').droppable({
        drop: function (event, ui) {
            var draggableObject = ui.draggable;
            var droppedAreaId = $(this).attr("id");
            var draggedAreaId = draggableObject.closest(".variableArea").attr("id");
            window.mixedModelApp.modelViewModel.dragHoverOut(droppedAreaId, draggableObject.text());
            setTimeout(function () {
                window.mixedModelApp.modelViewModel.moveVariable(draggedAreaId, droppedAreaId, draggableObject.text());
                window.mixedModelApp.modelViewModel.hasInternalHover(false);
            }, 0);
        },
        over: function (event, ui) {
            var draggableObject = ui.draggable;
            var droppedAreaId = $(this).attr("id");
            var draggedAreaId = draggableObject.closest(".variableArea").attr("id");
            if (draggedAreaId != droppedAreaId) {
                window.mixedModelApp.modelViewModel.dragHoverIn(droppedAreaId, draggableObject.text());
            };
        },
        out: function (event, ui) {
            var draggableObject = ui.draggable;
            var droppedAreaId = $(this).attr("id");
            var draggedAreaId = draggableObject.closest(".variableArea").attr("id");
            if (draggedAreaId != droppedAreaId) {
                window.mixedModelApp.modelViewModel.dragHoverOut(droppedAreaId, draggableObject.text());
            };
        }
    });
};

var applyDynamicDragObjects = function () {
    var hoverIndication = {};

    $('.variableBox').draggable({ cancel: ".firstVariableGroup", containment: "#variablePane", helper: 'clone' });
    $(".variableBox").bind("drag", function (event, ui) {
        ui.helper.css("background-color", "red");
        ui.helper.css("z-index", "1000");
    });
    $('.linearVariableGroup, .randomVariableGroup').droppable({
        drop: function (event, ui) {
            $(this).removeClass("droppedArea");
            if (window.mixedModelApp.modelViewModel.hasInternalHover()) {
                var draggableObject = ui.draggable;
                var droppedAreaId = $(this).attr("id");
                var draggedAreaId = draggableObject.closest(".variableArea").attr("id");
                setTimeout(function () { window.mixedModelApp.modelViewModel.moveVariable(draggedAreaId, droppedAreaId, draggableObject.text()); }, 0);
            };
        },
        over: function () {
            var variableGroupObject = $(this);
            var droppedAreaId = variableGroupObject.attr("id");
            hoverIndication[droppedAreaId] = true;
            setTimeout(function (variableGroupObjectInternal) {
                if (hoverIndication[droppedAreaId]) {
                    variableGroupObjectInternal.addClass("droppedArea");
                    window.mixedModelApp.modelViewModel.hasInternalHover(true);
                };
            }, 1000, variableGroupObject);
        },
        out: function () {
            var droppedAreaId = $(this).attr("id");
            hoverIndication[droppedAreaId] = false;
            $(this).removeClass("droppedArea");
            window.mixedModelApp.modelViewModel.hasInternalHover(false);
        }
    });
};

$(".optionsMenuItem").hover(function () {
    $(this).css("background-color", "#ffd800");
}, function () {
    $(this).css("background-color", "white");
});

var applyDraggable = function () {
    applyStaticDragObjects();
    applyDynamicDragObjects();
};

window.mixedModelApp.modelViewModel = (function (ko, datacontext) {
    /// <field name="todoLists" value="[new datacontext.todoList()]"></field>
    var self = this;

    var formulaObjects = {};
    var getFormula = function(formulaString) {
        if (!formulaObjects[formulaString]) {
            formulaObjects[formulaString] = new window.FormulaObject(formulaString, allVariables());
        };

        return formulaObjects[formulaString];
    };

    // Persisted properties
    self.formula = ko.observable();
    self.questions = ko.observableArray();

    // Non-persisted properties
    self.errorMessage = ko.observable();
    self.inProgress = ko.observable(false);
    self.isSiteLoading = ko.observable(true);
    self.isFileDialogOpen = ko.observable(false);
    self.uploadFileName = ko.observable();
    self.currentFileName = ko.observable(window.pageLoadFileName);
    self.allVariables = ko.observableArray();
    self.rowCount = ko.observable();
    self.tableAnalysis = ko.observable();
    self.samples = ko.observableArray();

    // Drag & Drop effects
    self.predictedVariableHover = ko.observable(false);
    self.linearEffectsHover = ko.observable(false);
    self.randomEffectsHover = ko.observable(false);
    self.excludedVariableHover = ko.observable(false);
    self.hasInternalHover = ko.observable(false);

    // Cumputed variables
    self.predictedVariable = ko.computed(function () {
        return getFormula(formula()).predictedVariable.slice(0); 
    });
    self.linearEffects = ko.computed(function () {
        return getFormula(formula()).linearEffects.slice(0);
    });
    self.randomEffects = ko.computed(function () {
        return getFormula(formula()).randomEffects.slice(0);
    });
    self.excludedVariables = ko.computed(function () {
        return getFormula(formula()).excludedVariables.slice(0);
    });
    self.selectedAnswer = ko.computed(function () {
        for (var i = 0; i < self.questions().length; i++) {
            if (self.questions()[i].selected()) {
                return self.questions()[i].answer;
            };
        };

        // Can this happen?!
        return null;
    });

    var setModel = function () {
        if (currentFileName() != "No File Selected") {
            datacontext.saveModel(viewModel);
        };
    };

    self.setNewModel = function (newModel) {
        self.formula(newModel);
        setModel();
    }

    var getModelAnalysis = function () {
        datacontext.getModelAnalysis(viewModel);
    };

    var analyzeModel = function () {
        viewModel.inProgress(true);
        viewModel.getModelAnalysis();
    };

    var firstEdit = true;
    var formulaEditCount = 0;
    var formulaEdited = function () {
        applyDynamicDragObjects();
        if (!firstEdit) {
            var currentFormulaEdit = ++formulaEditCount;
            setTimeout(function () {
                if (currentFormulaEdit === formulaEditCount) {
                    viewModel.inProgress(true);
                    viewModel.setModel();
                };
            }, 1000);
        }
        else {
            firstEdit = false;
        };
    };

    formula.subscribe(formulaEdited);

    $(document).mouseup(function (e) {
        var container = $("#optionsMenu");

        if (!container.is(e.target) // if the target of the click isn't the container...
            && container.has(e.target).length === 0) // ... nor a descendant of the container
        {
            if (!$("#cogMenu").is(e.target)) {
                container.hide();
            }
        }
    });

    var showOptionsMenu = function () {
        var container = $("#optionsMenu");
        container.toggle();
    };

    var showFileDialog = function () {
        isFileDialogOpen(true);
    };

    var hideFileDialog = function () {
        isFileDialogOpen(false);
    };

    var chooseFileToUpload = function () {
        $('#fileUploadChoose').click();
    };

    var fileChosen = function () {
        uploadFileName($('#fileUploadChoose')[0].files[0].name);
    };

    var submitUploadFile = function () {
        $('#fileUploadChoose').parent().parent().parent().parent()[0].submit();
        self.currentFileName(uploadFileName());
        self.formula("");
        self.clearQuestions();
        hideFileDialog();

        function pollNewFile() {
            inProgress(true);
            isSiteLoading(true);
            $('#downloadForm').hide();
            datacontext.getModel(viewModel).always(function(data) {
                if (data.fileName != uploadFileName()) {
                    setTimeout(pollNewFile, 0);
                } else {
                    isSiteLoading(false);
                }
            });
        };

        setTimeout(pollNewFile, 250);
    };

    self.submitUploadSample = function (sample) {
        var sampleName = sample.name.substr(0, sample.name.length-1);
        datacontext.setSample(sampleName);
        self.currentFileName(sampleName);
        hideFileDialog();

        function pollNewFile() {
            inProgress(true);
            isSiteLoading(true);
            $('#downloadForm').hide();
            datacontext.getModel(viewModel).always(function (data) {
                if (data.fileName != sampleName) {
                    setTimeout(pollNewFile, 0);
                } else {
                    isSiteLoading(false);
                }
            });
        };
        setTimeout(pollNewFile, 250);
    };

    var getHoverDragAreaObservale = function (areadId) {
        if (areadId == 'predictedVariableArea') {
            return self.predictedVariableHover;
        };

        if (areadId == 'linearEffectsArea') {
            return self.linearEffectsHover;
        };

        if (areadId == 'randomEffectsArea') {
            return self.randomEffectsHover;
        };

        if (areadId == 'excludedVariablesArea') {
            return self.excludedVariableHover;
        };

        return function () { };
    };

    self.dragHoverIn = function (droppedAreaId, variableName) {
        var observable = getHoverDragAreaObservale(droppedAreaId, variableName);
        observable(true);
    };

    self.dragHoverOut = function (droppedAreaId, variableName) {
        var observable = getHoverDragAreaObservale(droppedAreaId, variableName);
        observable(false);
    };

    var moveVariable = function (draggedAreaId, droppedAreaId, variableName) {
        // Don't move constants from random effect area
        if (variableName == "1" || variableName == "0") {
            return;
        }

        // Don't allow staying with no perdicted variable
        if (draggedAreaId == "predictedVariableArea") {
            alert("To replace perdicted variable drag another variable on top of it.");
            return;
        }

        // Reshape formula object
        var formulaObj = new window.FormulaObject(formula(), allVariables());
        formulaObj.moveVariable(draggedAreaId, droppedAreaId, variableName, hasInternalHover());

        // Check new formula is allowed 
        var formulaChnage = window.RunFormulaRules(new window.FormulaObject(formula(), allVariables()), formulaObj);
        if (formulaChnage == "") { // Formula change allowed

            // Rebuild formula after draggs
            formula(formulaObj.generateFormula());
        }
        else { // Formula change failed on some rule
            alert(formulaChnage);
        };
    };

    self.selectQuestion = function (item) {
        for (var i = 0; i < self.questions().length; i++) {
            self.questions()[i].selected(false);
        };

        item.selected(true);
    };

    self.clearQuestions = function () {
        self.questions([]);
    };

    self.clearQuestionsExceptFirst = function () {
        self.questions([self.questions()[0]]);
        self.questions()[0].selected(true);
    };

    self.addQuestion = function (question, answer) {
        var selected = (self.questions().length === 0) ? ko.observable(true) : ko.observable(false);
        self.questions.push({question: question, selected: selected, answer: answer });
    };

    self.addQuestions = function (questionItems) {
        $('#downloadForm').show();
        for (var i = 0; i < questionItems.length; i++) {
            self.addQuestion(questionItems[i].question, questionItems[i].answer);
        };
    };

    self.resetQuestions = function (modelInterpert, modelIntent, data) {
        self.clearQuestions();
        self.addQuestion("Model explanation:", modelInterpert);
        if (modelIntent && modelIntent != "") {
             self.addQuestion("Did I mean this model?", modelIntent);
        }
        self.addQuestion("The Data:", data);
    };

    var viewModel = {
        setModel: setModel,
        setNewModel: setNewModel,
        getModelAnalysis: getModelAnalysis,
        analyzeModel: analyzeModel,
        formulaEdited: formulaEdited,
        showOptionsMenu: showOptionsMenu,
        showFileDialog: showFileDialog,
        hideFileDialog: hideFileDialog,
        chooseFileToUpload: chooseFileToUpload,
        fileChosen: fileChosen,
        submitUploadFile: submitUploadFile,
        submitUploadSample: submitUploadSample,
        formula: formula,
        allVariables: allVariables,
        rowCount: rowCount,
        tableAnalysis: tableAnalysis,
        samples: samples,
        questions: questions,
        interpert: interpert,
        intent: intent,
        analysis: analysis,
        inProgress: inProgress,
        errorMessage: errorMessage,
        isFileDialogOpen: isFileDialogOpen,
        uploadFileName: uploadFileName,
        currentFileName: currentFileName,
        predictedVariable: predictedVariable,
        linearEffects: linearEffects,
        randomEffects: randomEffects,
        excludedVariables: excludedVariables,
        moveVariable: moveVariable,
        dragHoverIn: dragHoverIn,
        dragHoverOut: dragHoverOut,
        hasInternalHover: hasInternalHover,
        selectQuestion: selectQuestion,
        clearQuestions: clearQuestions,
        addQuestion: addQuestion,
        resetQuestions: resetQuestions,
        clearQuestionsExceptFirst: clearQuestionsExceptFirst,
        addQuestions: addQuestions
    };

    datacontext.getSamples(viewModel);
    $('#downloadForm').hide();
    if (currentFileName() != "No File Selected") {
        datacontext.getModel(viewModel).always(function () { self.isSiteLoading(false); }); // load model
    } else {
        self.isSiteLoading(false); // remove overlay for user to select a file
    };

    return viewModel;

})(ko, mixedModelApp.datacontext);

// Initiate the Knockout bindings
ko.applyBindings(window.mixedModelApp.modelViewModel);

applyDraggable();
