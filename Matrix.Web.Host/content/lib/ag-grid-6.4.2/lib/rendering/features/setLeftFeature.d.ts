// Type definitions for ag-grid v6.4.2
// Project: http://www.ag-grid.com/
// Definitions by: Niall Crosby <https://github.com/ceolter/>
// Definitions: https://github.com/borisyankov/DefinitelyTyped
import { ColumnGroupChild } from "../../entities/columnGroupChild";
export declare class SetLeftFeature {
    private columnOrGroup;
    private eCell;
    private destroyFunctions;
    constructor(columnOrGroup: ColumnGroupChild, eCell: HTMLElement);
    private init();
    private onLeftChanged();
    destroy(): void;
}
