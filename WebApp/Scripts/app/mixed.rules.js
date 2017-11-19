Array.prototype.contains = function (obj) {
    var i = this.length;
    while (i--) {
        if (this[i] == obj) {
            return true;
        }
    }
    return false;
}

window.RunFormulaRules = function (oldFormulaObject, newFormulaObject) {

    // Local index declarations
    var i;
    var rowCount = window.mixedModelApp.modelViewModel.rowCount();
    var tableAnalysis = window.mixedModelApp.modelViewModel.tableAnalysis();

    // If predicting variable had changed, check that new predicting variable is of decimal type
    //if (oldFormulaObject.predictedVariable[0].variableName != newFormulaObject.predictedVariable[0].variableName) {
    //    if (newFormulaObject.variableDescriptors[newFormulaObject.predictedVariable[0].variableName].type != 1) {
    //        return "Predicting variable needs to have only decimal values";
    //    };
    //};
    // Track change in fixed/random part to detect aliasing later 
    var changedInFixedPart = false;
    var changedInRandomPart = false;

    // If a new random effect is introduced, check that is is a level variable
    if (newFormulaObject.randomEffects.length > oldFormulaObject.randomEffects.length) {
        for (i = 0; i < newFormulaObject.randomEffects.length; i++) {
            if (i == newFormulaObject.randomEffects.length - 1 ||
                newFormulaObject.randomEffects[i].variableName != oldFormulaObject.randomEffects[i].variableName) {
                changedInRandomPart = true;

                if (newFormulaObject.variableDescriptors[newFormulaObject.randomEffects[i].variableName].valueCount == rowCount) {
                    return "Random effects should have less levels than dataset count (N=" + rowCount + ")";
                };

                if (tableAnalysis.columnRepeated[newFormulaObject.randomEffects[i].variableName] == ColumnRepeated.NonRepeated) {
                    return "Random effects should be repeated. This item is repeated on less than 80% of its values";
                };

                if (newFormulaObject.variableDescriptors[newFormulaObject.randomEffects[i].variableName].valueCount == 1) {
                    return "Random effects should have more than one level";
                };

                if (newFormulaObject.variableDescriptors[newFormulaObject.randomEffects[i].variableName].valueCount < 5) {
                    return "Random effects with less than 5 levels are not supported since variable will no be estimated properly";
                };
            };
        };
    };

    // Adding a fixed effect
    if (newFormulaObject.linearEffects.length > oldFormulaObject.linearEffects.length) {
        changedInFixedPart = true;
        if (newFormulaObject.variableDescriptors[newFormulaObject.linearEffects[newFormulaObject.linearEffects.length - 1].variableNames[0].variableName].valueCount > 15 &&
            newFormulaObject.variableDescriptors[newFormulaObject.linearEffects[newFormulaObject.linearEffects.length - 1].variableNames[0].variableName].type != 1) {
            return "Fixed effects with more than 15 levels are less likely, consider adding variable as a random effect. If you're sure you want a fixed-effect, please change connfiguration manually.";
        };
        if (newFormulaObject.variableDescriptors[newFormulaObject.linearEffects[newFormulaObject.linearEffects.length - 1].variableNames[0].variableName].valueCount == 1) {
            return "Fixed effects should have more than one value.";
        };
        if (newFormulaObject.linearEffects[newFormulaObject.linearEffects.length - 1].variableNames[0].variableName == "0" ||
            newFormulaObject.linearEffects[newFormulaObject.linearEffects.length - 1].variableNames[0].variableName == "1") {
            return "0, 1 are meant only as covariate for random part.";
        };
        for (i = 0; i < oldFormulaObject.linearEffects.length; i++) {
            for (var j = 0; j < oldFormulaObject.linearEffects[i].variableNames.length; j++) {
                if (oldFormulaObject.linearEffects[i].variableNames[j].variableName == 
                    newFormulaObject.linearEffects[newFormulaObject.linearEffects.length - 1].variableNames[0].variableName) {
                    return "Fixed effect is already included.";
                };
            };
        };
    };

    // Check whether random effects components were added
    if (oldFormulaObject.randomEffects.length == newFormulaObject.randomEffects.length) {
        var randomFormulaChanged = false;
        var changedRandomEffectGroup = -1;
        for (i = 0; i < newFormulaObject.randomEffects.length; i++) {
            if (newFormulaObject.randomEffects[i].variableNames.length != oldFormulaObject.randomEffects[i].variableNames.length) {
                randomFormulaChanged = true;
                changedRandomEffectGroup = i;
            };
        };

        if (randomFormulaChanged) {
            var covariates = newFormulaObject.randomEffects[changedRandomEffectGroup].variableNames;
            var addedVariables = {};

            if (covariates.length > 0 &&
                newFormulaObject.variableDescriptors[newFormulaObject.predictedVariable[0].variableName].type != 1) {
                return "Covarites are only allowed in with numeric predited values";
            }

            for (i = 0; i < covariates.length; i++) {
                if (covariates[i].variableName in addedVariables) {
                    return "Covariate can only appear once in each random group";
                }
                addedVariables[covariates[i].variableName] = true;

                if (covariates[i].variableName != "1" && covariates[i].variableName != "0" &&
                    newFormulaObject.variableDescriptors[covariates[i].variableName].type != 1) {
                    return "Only numeric random effects covariates are supported";
                };
            }
        };
    };

    // Check whether fixed effects components were added
    if (oldFormulaObject.linearEffects.length == newFormulaObject.linearEffects.length) {
        var changedFixedFormulas = -1;
        for (i = 0; i < oldFormulaObject.linearEffects.length; i++) {
            if (newFormulaObject.linearEffects[i].variableNames.length > oldFormulaObject.linearEffects[i].variableNames.length) {
                changedFixedFormulas = i;
            };
        };

        if (changedFixedFormulas >= 0) {
            changedInFixedPart = true;

            var decimalVariableCount = 0;
            for (var j = 0; j < newFormulaObject.linearEffects[changedFixedFormulas].variableNames.length; j++) {
                if (newFormulaObject.linearEffects[changedFixedFormulas].variableNames[j] != oldFormulaObject.linearEffects[changedFixedFormulas].variableNames[j]) {
                    if (newFormulaObject.variableDescriptors[newFormulaObject.linearEffects[changedFixedFormulas].variableNames[j].variableName].type == 1) {
                        decimalVariableCount++;
                    }
                    else if (newFormulaObject.variableDescriptors[newFormulaObject.linearEffects[changedFixedFormulas].variableNames[j].variableName].valueCount > 15) {
                        return "Fixed effects with more than 15 levels are less likely, consider adding variable as a random effect. If you're sure you want a fixed-effect, please change connfiguration manually.";
                    };
                };
            };

            if (decimalVariableCount > 1) {
                return "Multiplication of two or more decimal variables is not allowed";
            };

            var variableNames = {};
            for (j = 0; j < newFormulaObject.linearEffects[changedFixedFormulas].variableNames.length; j++) {
                if (newFormulaObject.linearEffects[changedFixedFormulas].variableNames[j].variableName in variableNames) {
                    return "Repeating same variable in same interaction is not allowed.";
                }

                variableNames[newFormulaObject.linearEffects[changedFixedFormulas].variableNames[j].variableName] = true;

                if (newFormulaObject.linearEffects[changedFixedFormulas].variableNames[j] == "0" ||
                    newFormulaObject.linearEffects[changedFixedFormulas].variableNames[j] == "1") {
                    return "Only numeric random effects covariates are supported";
                }

                if (newFormulaObject.linearEffects[changedFixedFormulas].variableNames[j] != oldFormulaObject.linearEffects[changedFixedFormulas].variableNames[j]) {
                    if (newFormulaObject.variableDescriptors[newFormulaObject.linearEffects[changedFixedFormulas].variableNames[j].variableName].type == 1) {
                        decimalVariableCount++;
                    }
                    else if (newFormulaObject.variableDescriptors[newFormulaObject.linearEffects[changedFixedFormulas].variableNames[j].variableName].valueCount > 15) {
                        return "Fixed effects with more than 15 levels are less likely, consider adding variable as a random effect. If you're sure you want a fixed-effect, please change connfiguration manually.";
                    };
                };
            };
        };
    };

    // Check nesting violation 
    if (changedInFixedPart || changedInRandomPart) {
        var fixedVariables = {};
        for (i = 0; i < newFormulaObject.linearEffects.length; i++) {
            for (j = 0; j < newFormulaObject.linearEffects[i].variableNames.length; j++) {
                fixedVariables[newFormulaObject.linearEffects[i].variableNames[j].variableName] = true;
            };
        };

        for (var key in fixedVariables) {
            var relations = tableAnalysis.columnGraph[key];
            for (var relatedVar in relations) {
                var relationIndex;
                if (relatedVar in fixedVariables) {
                    for (relationIndex in relations[relatedVar].relationAttributes) {
                        if (LinkAttributes.Nested == relations[relatedVar].relationAttributes[relationIndex]) {
                            return "" + key + " is nested in " + relatedVar + ". Adding both as fixed effects will surely cause aliasing in regression.";
                        };
                    }
                };
                if ($.map(newFormulaObject.randomEffects, function (itm) { return itm.variableName; }).contains(relatedVar)) {
                    for (relationIndex in relations[relatedVar].relationAttributes) {
                        if (LinkAttributes.Nested == relations[relatedVar].relationAttributes[relationIndex]) {
                            return "" + key + " is nested in " + relatedVar + ". This will probably cause variance estimate for " + relatedVar + " to be very low.";
                        };
                    }
                };
            };
        };
    };

    // No rule was hit allow formula change
    return "";
};
