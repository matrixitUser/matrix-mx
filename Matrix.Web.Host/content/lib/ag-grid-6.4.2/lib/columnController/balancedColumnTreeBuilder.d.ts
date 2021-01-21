// Type definitions for ag-grid v6.4.2
// Project: http://www.ag-grid.com/
// Definitions by: Niall Crosby <https://github.com/ceolter/>
// Definitions: https://github.com/borisyankov/DefinitelyTyped
import { ColGroupDef } from "../entities/colDef";
import { ColDef } from "../entities/colDef";
export declare class BalancedColumnTreeBuilder {
    private gridOptionsWrapper;
    private columnUtils;
    private context;
    private logger;
    private setBeans(loggerFactory);
    createBalancedColumnGroups(abstractColDefs: (ColDef | ColGroupDef)[], primaryColumns: boolean): any;
    private balanceColumnTree(unbalancedTree, currentDept, columnDept, columnKeyCreator);
    private findMaxDept(treeChildren, dept);
    private recursivelyCreateColumns(abstractColDefs, level, columnKeyCreator, primaryColumns);
    private createColumnGroup(columnKeyCreator, primaryColumns, colGroupDef, level);
    private createMergedColGroupDef(colGroupDef);
    private createColumn(columnKeyCreator, primaryColumns, colDef3);
    private checkForDeprecatedItems(colDef);
    private isColumnGroup(abstractColDef);
}
