/// <reference path="../../common/definition/index.d.ts" />

namespace Ctail.Training.WebResources.Entities.Training {

    /**
     * Library responsible for form scripting on Training entity
     * */
    export class FormLibrary {

        /**
         * Called on load of the form
         * @param {Xrm.Events.EventContext} executionContext
         * @param {string} params = "name1:datatype,name2:datatype"
         */
        onLoad(executionContext: Xrm.Events.EventContext, params: string): void {
            let functionName: string = "onLoad";
            let formContext: Xrm.FormContext;
            try {
                //Get the form context
                formContext = executionContext.getFormContext();

                //Validate if the Training has the value, which means this is a subtraining
                if (this.isValid(formContext.getAttribute("ctail_training")) && this.isValid(formContext.getAttribute("ctail_training").getValue())) {
                    //Split the parameter by comma
                    let paramsArray: string[] = params.split(",");
                    //Loop through the entire array
                    paramsArray.forEach((value, index) => {
                        let valueArray: string[] = value.split(":");
                        this.showElement(valueArray[0], valueArray[1], false, formContext);
                    });
                }
            } catch (e) {
                console.error(`${functionName}: ${e.message}`);
            }
        }

        /**
         * Generic function to show/hide elements
         * @param {string} name
         * @param {string} type
         * @param {boolean} show
         * @param {Xrm.FormContext} formContext
         */
        showElement(name: string, type: string, show: boolean, formContext: Xrm.FormContext): void {
            let functionName: string = "showElement";
            try {
                switch (type) {
                    case 'column':
                        break;
                    case 'tab':
                        if (this.isValid(formContext.ui.tabs.get(name)))
                            formContext.ui.tabs.get(name).setVisible(show);
                        break;
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

let Ctail_TrainingFormLibrary = new Ctail.Training.WebResources.Entities.Training.FormLibrary();