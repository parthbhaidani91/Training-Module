/// <reference path="../../common/definition/index.d.ts" />
var Ctail;
(function (Ctail) {
    var Training;
    (function (Training) {
        var WebResources;
        (function (WebResources) {
            var Entities;
            (function (Entities) {
                var TrainigSlot;
                (function (TrainigSlot) {
                    /**
                     * Library responsible for form scripting on Training entity
                     * */
                    var FormLibrary = /** @class */ (function () {
                        function FormLibrary() {
                        }
                        /**
                         * Function to be called on change of the Scheduled Date column
                         * @param executionContext
                         */
                        FormLibrary.prototype.onChangeScheduledDate = function (executionContext) {
                            var functionName = "onChangeScheduledDate";
                            var formContext;
                            try {
                                //Get the form context
                                formContext = executionContext.getFormContext();
                                //Get the Scheduled Date value
                                if (this.isValid(formContext.getAttribute("ctail_scheduleddate")) && this.isValid(formContext.getAttribute("ctail_scheduleddate").getValue())) {
                                    var scheduledDate = formContext.getAttribute("ctail_scheduleddate").getValue();
                                    var currentDateTime = new Date();
                                    var scheduledDateControl = formContext.getControl("ctail_scheduleddate");
                                    //If the Scheduled Date is lesser than Current Date, then throw error
                                    if (scheduledDate < currentDateTime) {
                                        scheduledDateControl.setNotification("Entered Date and Time cannot be lesser than current Date and Time", "ScheduledDate");
                                    }
                                    else {
                                        scheduledDateControl.clearNotification("ScheduledDate");
                                    }
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
                    TrainigSlot.FormLibrary = FormLibrary;
                })(TrainigSlot = Entities.TrainigSlot || (Entities.TrainigSlot = {}));
            })(Entities = WebResources.Entities || (WebResources.Entities = {}));
        })(WebResources = Training.WebResources || (Training.WebResources = {}));
    })(Training = Ctail.Training || (Ctail.Training = {}));
})(Ctail || (Ctail = {}));
var Ctail_TrainingSlotFormLibrary = new Ctail.Training.WebResources.Entities.TrainigSlot.FormLibrary();
//# sourceMappingURL=FormLibrary.js.map