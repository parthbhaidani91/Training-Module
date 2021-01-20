/// <reference path="../../common/definition/index.d.ts" />

namespace Ctail.Training.WebResources.Entities.TrainigSlot {

    /**
     * Library responsible for form scripting on Training entity
     * */
    export class FormLibrary {

        /**
         * Function to be called on change of the Scheduled Date column
         * @param executionContext
         */
        onChangeScheduledDate(executionContext: Xrm.Events.EventContext): void {
            let functionName: string = "onChangeScheduledDate";
            let formContext: Xrm.FormContext;
            try {
                //Get the form context
                formContext = executionContext.getFormContext();

                //Get the Scheduled Date value
                if (this.isValid(formContext.getAttribute("ctail_scheduleddate")) && this.isValid(formContext.getAttribute("ctail_scheduleddate").getValue())) {
                    let scheduledDate: Date = formContext.getAttribute("ctail_scheduleddate").getValue();
                    let currentDateTime: Date = new Date();
                    let scheduledDateControl: Xrm.Controls.DateControl = formContext.getControl("ctail_scheduleddate");

                    //If the Scheduled Date is lesser than Current Date, then throw error
                    if (scheduledDate < currentDateTime) {
                        scheduledDateControl.setNotification("Entered Date and Time cannot be lesser than current Date and Time", "ScheduledDate")
                    }
                    else {
                        scheduledDateControl.clearNotification("ScheduledDate");
                    }
                }
            } catch (e) {
                console.error(`${functionName}: ${e.message}`);
            }
        }

        /**
         * Generic function to validate the items passed to it
         * @param {any} item
         * @returns {boolean} indicates whether the passed item is a valid item or not
         */
        isValid(item: any): boolean {
            let functionName: string = "isValid";
            let valid: boolean = false;
            try {
                if (item != null && item != undefined && item != "undefined" && item != "null" && item != "")
                    valid = true;
            } catch (e) {
                console.error(`${functionName}: ${e.message}`);
            }
            return valid;
        }
    }
}

let Ctail_TrainingSlotFormLibrary = new Ctail.Training.WebResources.Entities.TrainigSlot.FormLibrary();