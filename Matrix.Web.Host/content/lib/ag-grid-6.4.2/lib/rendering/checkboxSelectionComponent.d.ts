// Type definitions for ag-grid v6.4.2
// Project: http://www.ag-grid.com/
// Definitions by: Niall Crosby <https://github.com/ceolter/>
// Definitions: https://github.com/borisyankov/DefinitelyTyped
import { Component } from "../widgets/component";
export declare class CheckboxSelectionComponent extends Component {
    private gridOptionsWrapper;
    private eCheckedIcon;
    private eUncheckedIcon;
    private eIndeterminateIcon;
    private rowNode;
    constructor();
    private createAndAddIcons();
    private onSelectionChanged();
    private onCheckedClicked();
    private onUncheckedClicked(event);
    private onIndeterminateClicked(event);
    init(params: any): void;
}
