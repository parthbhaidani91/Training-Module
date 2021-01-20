/// <reference path="../../common/definition/index.d.ts" />
var Ctail;
(function (Ctail) {
    var Training;
    (function (Training_1) {
        var WebResources;
        (function (WebResources) {
            var Entities;
            (function (Entities) {
                var Training;
                (function (Training) {
                    /**
                     * Library responsible for form scripting on Training entity
                     * */
                    var FormLibrary = /** @class */ (function () {
                        function FormLibrary() {
                        }
                        /**
                         * Called on load of the form
                         * @param {Xrm.Events.EventContext} executionContext
                         * @param {string} params = "name1:datatype,name2:datatype"
                         */
                        FormLibrary.prototype.onLoad = function (executionContext, params) {
                            var _this = this;
                            var functionName = "onLoad";
                            var formContext;
                            try {
                                //Get the form context
                                formContext = executionContext.getFormContext();
                                //Validate if the Training has the value, which means this is a subtraining
                                if (this.isValid(formContext.getAttribute("ctail_training")) && this.isValid(formContext.getAttribute("ctail_training").getValue())) {
                                    //Split the parameter by comma
                                    var paramsArray = params.split(",");
                                    //Loop through the entire array
                                    paramsArray.forEach(function (value, index) {
                                        var valueArray = value.split(":");
                                        _this.showElement(valueArray[0], valueArray[1], false, formContext);
                                    });
                                }
                            }
                            catch (e) {
                                console.error(functionName + ": " + e.message);
                            }
                        };
                        /**
                         * Generic function to show/hide elements
                         * @param {string} name
                         * @param {string} type
                         * @param {boolean} show
                         * @param {Xrm.FormContext} formContext
                         */
                        FormLibrary.prototype.showElement = function (name, type, show, formContext) {
                            var functionName = "showElement";
                            try {
                                switch (type) {
                                    case 'column':
                                        break;
                                    case 'tab':
                                        if (this.isValid(formContext.ui.tabs.get(name)))
                                            formContext.ui.tabs.get(name).setVisible(show);
                                        break;
                                }
                            }
                            catch (e) {
                                console.error(functionName + ": " + e.message);
                            }
                        };
                        /**
                         * Generic function to validate the items passed to it
                         * @param {any} item
                         * @returns {boolean} indicates whether the passed item is a valid item or not
                         */
                        FormLibrary.prototype.isValid = function (item) {
                            var functionName = "isValid";
                            var valid = false;
                            try {
                                if (item != null && item != undefined && item != "undefined" && item != "null" && item != "")
                                    valid = true;
                            }
                            catch (e) {
                                console.error(functionName + ": " + e.message);
                            }
                            return valid;
                        };
                        return FormLibrary;
                    }());
                    Training.FormLibrary = FormLibrary;
                })(Training = Entities.Training || (Entities.Training = {}));
            })(Entities = WebResources.Entities || (WebResources.Entities = {}));
        })(WebResources = Training_1.WebResources || (Training_1.WebResources = {}));
    })(Training = Ctail.Training || (Ctail.Training = {}));
})(Ctail || (Ctail = {}));
var Ctail_TrainingFormLibrary = new Ctail.Training.WebResources.Entities.Training.FormLibrary();
//# sourceMappingURL=FormLibrary.js.map