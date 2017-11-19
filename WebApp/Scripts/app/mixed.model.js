// Table analysis constants
var ColumnClassification =
{
    UniqueId: 0,
    SingleValue: 1,
    Measure: 2,
    Regressor: 3,
    Grouping: 4,
};

var ColumnBalanace = 
{
    Balanced: 0,
    SemiBalanced: 1,
    NonBalanaced: 2,
};

var LinkAttributes =
{
    None: 0,
    Nested: 1,
    StrictCovering: 2,
    StatisticCovering: 3,
};

var ColumnRepeated =
{
    NonRepeated: 0,
    StrictRepeated: 1,
    StatisticRepeated: 2,
};

// Mixed model object
(function (ko, datacontext) {
    datacontext.model = mixedModel();

    function mixedModel(data) {
        var self = this;
        data = data || {};

        // Persisted properties
        self.model = ko.observable(data.model);
        self.interpert = ko.observable(data.interpert);
        self.intent = ko.observable(data.intent);
        self.analysis = ko.observable();

        // Non-persisted properties
        self.errorMessage = ko.observable();

        // Auto-save when these properties change
        // self.isDone.subscribe(saveChanges);
        // self.title.subscribe(saveChanges);

        self.toJson = function () { return ko.toJSON(self); };

        return self;
    };
})(ko, mixedModelApp.datacontext);

window.FormulaObject = function (formula, allVariables) {
    var self = this;
    self.predictedVariable = [];
    self.linearEffects = [];
    self.randomEffects = [];
    self.excludedVariables = $.map(allVariables, function (item) { return { variableName: item.name }; });
    self.variableDescriptors = {};
    for (var l = 0; l < allVariables.length; l++) {
        self.variableDescriptors[allVariables[l].name] = allVariables[l];
    }

    var serachIndex = function(variableName, arr) {
        for (var idx = 0; idx < arr.length; idx++) {
            if (arr[idx].variableName == variableName) {
                return idx;
            }
        }
        return -1;
    };

    if (formula) {
        var formulaParts = formula.split("~");
        if (formulaParts.length === 2) {
            self.predictedVariable = [{ variableName: formulaParts[0].trim() }];

            var regExp = /\(([^)]+)\)/g;
            var match = regExp.exec(formulaParts[1]);
            while (match != null) {
                self.randomEffects.push(
                    {
                        variableName: match[1].split("|")[1].replace(")", "").trim(),
                        variableNames: $.map(match[1].split("|")[0].split("+"),
                                                function (item) {
                                                    return { variableName: item.replace("(", "").trim() };
                                                })
                    });
                match = regExp.exec(formulaParts[1]);
            }

            var linearPart = formulaParts[1].indexOf('(') > -1 ? formulaParts[1].substring(0, formulaParts[1].indexOf('(') - 2) : formulaParts[1];
            self.linearEffects = $.map(ko.utils.arrayFilter(linearPart.split("+"), function (item) { return item.trim() != ""; }),
                                        function (item) {
                                            return {
                                                variableNames: $.map(item.split("*"),
                                                                    function (itm) {
                                                                        return { variableName: itm.trim() };
                                                                    })
                                            };
                                        });
        }

        self.excludedVariables.splice(serachIndex(self.predictedVariable[0].variableName, self.excludedVariables), 1);
        var i; var j;
        for (i = 0; i < self.linearEffects.length; i++) {
            for (j = 0; j < self.linearEffects[i].variableNames.length; j++) {
                self.excludedVariables.splice(serachIndex(self.linearEffects[i].variableNames[j].variableName, self.excludedVariables), 1);
            }
        }
        for (i = 0; i < self.randomEffects.length; i++) {
            self.excludedVariables.splice(serachIndex(self.randomEffects[i].variableName, self.excludedVariables), 1);
            for (j = 0; j < self.randomEffects[i].variableNames.length; j++) {
                if (self.randomEffects[i].variableNames[j].variableName != "1") {
                    self.excludedVariables.splice(serachIndex(self.randomEffects[i].variableNames[j].variableName, self.excludedVariables), 1);
                }
            }
        }
    }

    self.getArrayFromAreaId = function (areaId) {
        if (areaId === 'predictedVariableArea') {
            return self.predictedVariable;
        };

        if (areaId === 'linearEffectsArea') {
            return self.linearEffects;
        };

        if (areaId === 'randomEffectsArea') {
            return self.randomEffects;
        };

        if (areaId === 'excludedVariablesArea') {
            return self.excludedVariables;
        };

        // Can this happen?!
        return null;
    };

    self.removeVariableFromGroup = function (removeAreaId, removeVariable) {
        if (removeAreaId == 'randomEffectsArea') {
            if (self.getArrayFromAreaId(removeAreaId)[0].variableName == removeVariable) {
                self.getArrayFromAreaId(removeAreaId).splice(serachIndex(removeVariable, self.getArrayFromAreaId(removeAreaId)), 1);
            } else {
                for (var ind = 0; ind < self.getArrayFromAreaId(removeAreaId).length; ind++) {
                    if (serachIndex(removeVariable, self.getArrayFromAreaId(removeAreaId)[ind].variableNames) != -1) {
                        self.getArrayFromAreaId(removeAreaId)[ind].variableNames.splice(serachIndex(removeVariable, self.getArrayFromAreaId(removeAreaId)[ind]), 1);
                    }
                }
            }
        }
        else {
            self.linearEffects = $.map(self.linearEffects,
                                        function (item) {
                                            var searchedIndex = serachIndex(removeVariable, item.variableNames);
                                            if (searchedIndex > -1) {
                                                item.variableNames.splice(searchedIndex, 1);
                                            }
                                            return item;
                                        });
            ;
        };
    };

    self.addvariableToGroup = function (addAreaId, addVariable, hasInternalHover) {
        var linearEffectGroupIndex;
        if (addAreaId.indexOf("linearVariableGroup") == 0) {
            linearEffectGroupIndex = parseInt(addAreaId.split("-")[1]);
            self.linearEffects[linearEffectGroupIndex].variableNames.push({ variableName: addVariable });
        }
        else if (addAreaId.indexOf("randomVariableGroup") == 0) {
            linearEffectGroupIndex = parseInt(addAreaId.split("-")[1]);
            self.randomEffects[linearEffectGroupIndex].variableNames.push({ variableName: addVariable });
        }
        else if (hasInternalHover) {  // In this case we should not propagate event to parent area
            return;
        }
        else if (addAreaId === 'linearEffectsArea') {
            self.linearEffects.push({ variableNames: [{ variableName: addVariable }] });
        }
        else if (addAreaId === 'randomEffectsArea') {
            self.randomEffects.push({ variableName: addVariable, variableNames: [{ variableName: "1" }] });
        }
        else {
            self.getArrayFromAreaId(addAreaId).push({ variableName: addVariable });
        };
    };

    self.moveVariable = function(draggedAreaId, droppedAreaId, variableName, hasInternalHover) {
        // Suppress dragging from area to itself and from perdicted area in general
        if (draggedAreaId === 'predictedVariableArea' || draggedAreaId === droppedAreaId) {
            // Do nothing
            return;
        };

        // When dragging to predicted area previous predicted variable goes to excluded list
        if (droppedAreaId === 'predictedVariableArea') {
            self.excludedVariables.push(self.predictedVariable.pop());
        };

        // Move variable from dragged group to dropped group

        var skipRemoval = false;
        if (droppedAreaId.indexOf("randomEffectsArea") == 0 && hasInternalHover) {
            skipRemoval = true;
        };
        if (droppedAreaId.indexOf("randomVariableGroup") == 0) {
            skipRemoval = true;
        };
        /*if (draggedAreaId.indexOf("randomEffectsArea") == 0 && hasInternalHover) {
            skipRemoval = true;
        };*/
        if (draggedAreaId.indexOf("randomVariableGroup") == 0 && droppedAreaId.indexOf("randomVariableGroup") == 0) {
            skipRemoval = true;
        };

        if (!skipRemoval) { self.removeVariableFromGroup(draggedAreaId, variableName); };
        self.addvariableToGroup(droppedAreaId, variableName, hasInternalHover);
    };

    self.generateFormula = function () {
        var formulaText = self.predictedVariable[0].variableName + " ~ ";
        formulaText += $.map(ko.utils.arrayFilter(self.linearEffects,
                                                    function (item) { return item.variableNames.length > 0; }),
                                function (item) {
                                    return $.map(item.variableNames,
                                                function (variableItem) {
                                                    return variableItem.variableName;
                                                }).join(" * ");
                                }).
                            concat($.map(self.randomEffects,
                                        function (item) {
                                            return "(" + $.map(item.variableNames,
                                                                function (randomVar) {
                                                                    return randomVar.variableName;
                                                                }).join("+") +
                                                    "|" + item.variableName + ")";
                                        })).
                            join(" + ");
        return formulaText;
    };

};