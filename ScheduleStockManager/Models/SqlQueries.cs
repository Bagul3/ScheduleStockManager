namespace ScheduleStockManager.Models
{
    public static class SqlQueries
    {
        public static string StockQuery => @"SELECT ([T2_BRA].[REF] + [F7]) AS NewStyle,
			Sum(T2_BRA.Q11) AS QTY1, Sum(T2_BRA.Q12) AS QTY2, 
                Sum(T2_BRA.Q13) AS QTY3, Sum(T2_BRA.Q14) AS QTY4, Sum(T2_BRA.Q15) AS QTY5, Sum(T2_BRA.Q16) AS QTY6, Sum(T2_BRA.Q17) AS QTY7, Sum(T2_BRA.Q18) AS QTY8, 
                    Sum(T2_BRA.Q19) AS QTY9, Sum(T2_BRA.Q20) AS QTY10, Sum(T2_BRA.Q21) AS QTY11, Sum(T2_BRA.Q22) AS QTY12, Sum(T2_BRA.Q23) AS QTY13, T2_HEAD.REF,
                        Sum(T2_BRA.LY11) AS LY1, Sum(T2_BRA.LY12) AS LY2, Sum(T2_BRA.LY13) AS LY3, Sum(T2_BRA.LY14) AS LY4, Sum(T2_BRA.LY15) AS LY5, 
                            Sum(T2_BRA.LY16) AS LY6, Sum(T2_BRA.LY17) AS LY7, Sum(T2_BRA.LY18) AS LY8, Sum(T2_BRA.LY19) AS LY9, Sum(T2_BRA.LY20) AS LY10,
                                Sum(T2_BRA.LY21) AS LY11, Sum(T2_BRA.LY22) AS LY12, Sum(T2_BRA.LY23) AS LY13, T2_HEAD.LASTDELV, T2_HEAD.SELL, T2_HEAD.REM, T2_HEAD.REM2, T2_HEAD.USER1
									FROM ((((T2_BRA INNER JOIN T2_HEAD ON T2_BRA.REF = T2_HEAD.REF)) INNER JOIN 
									(SELECT Right(t.[KEY],3) AS NewCol, t.F1 AS MasterColour, Left(t.[KEY],3) AS Col, t.F7 
								FROM T2_LOOK t
								WHERE (Left(t.[KEY],3))='COL') as Colour ON T2_BRA.COLOUR = Colour.NewCol) INNER JOIN [DESC] ON [T2_BRA].[REF] = [DESC].SKU)
                                    WHERE T2_BRA.BRANCH in ('A','G')
									GROUP BY ([T2_BRA].[REF] + [F7]),T2_HEAD.REF, T2_HEAD.LASTDELV, T2_HEAD.SELL, T2_HEAD.REM, T2_HEAD.REM2, T2_HEAD.USER1";

		public static string StockQueryALL => @"SELECT ([T2_BRA].[REF] + [F7]) AS NEWSTYLE, Suppliers.MasterSupplier, Dept.MasterDept, Colour.MasterColour, Colour.F7, T2_HEAD.SHORT, 
	T2_HEAD.[DESC], T2_HEAD.[GROUP], T2_HEAD.STYPE, T2_HEAD.SIZERANGE, T2_HEAD.SUPPLIER, T2_HEAD.SUPPREF, T2_HEAD.VAT, T2_HEAD.REM, T2_HEAD.REM2, T2_HEAD.BASESELL, T2_HEAD.SELL, T2_HEAD.LASTDELV,
		T2_HEAD.SELLB, T2_HEAD.SELL1, Sum(T2_BRA.Q11) AS QTY1, Sum(T2_BRA.Q12) AS QTY2, Sum(T2_BRA.Q13) AS QTY3, 
			Sum(T2_BRA.Q14) AS QTY4, Sum(T2_BRA.Q15) AS QTY5, Sum(T2_BRA.Q16) AS QTY6, Sum(T2_BRA.Q17) AS QTY7, Sum(T2_BRA.Q18) AS QTY8, 
				Sum(T2_BRA.Q19) AS QTY9, Sum(T2_BRA.Q20) AS QTY10, Sum(T2_BRA.Q21) AS QTY11, Sum(T2_BRA.Q22) AS QTY12, Sum(T2_BRA.Q23) AS QTY13, 
					T2_HEAD.REF,Stocktype.MasterStocktype,SubDept.MasterSubDept, T2_HEAD.USER1,
                        Sum(T2_BRA.LY11) AS LY1, Sum(T2_BRA.LY12) AS LY2, Sum(T2_BRA.LY13) AS LY3, Sum(T2_BRA.LY14) AS LY4, Sum(T2_BRA.LY15) AS LY5, 
                            Sum(T2_BRA.LY16) AS LY6, Sum(T2_BRA.LY17) AS LY7, Sum(T2_BRA.LY18) AS LY8, Sum(T2_BRA.LY19) AS LY9, Sum(T2_BRA.LY20) AS LY10, 
                                Sum(T2_BRA.LY21) AS LY11, Sum(T2_BRA.LY22) AS LY12, Sum(T2_BRA.LY23) AS LY13, T2_HEAD.VAT, T2_SIZES.S01, T2_SIZES.S02, T2_SIZES.S03, T2_SIZES.S04, T2_SIZES.S05, T2_SIZES.S06, T2_SIZES.S07, T2_SIZES.S08, T2_SIZES.S09, T2_SIZES.S10, T2_SIZES.S11, T2_SIZES.S12, T2_SIZES.S13
									FROM (((((((T2_BRA INNER JOIN T2_HEAD ON T2_BRA.REF = T2_HEAD.REF) INNER JOIN (SELECT Right(T2_LOOK.[KEY],3) AS NewCol, T2_LOOK.F1 AS MasterColour, Left(T2_LOOK.[KEY],3) AS Col, T2_LOOK.F7
								FROM T2_LOOK
								WHERE (Left(T2_LOOK.[KEY],3))='COL') as Colour ON T2_BRA.COLOUR = Colour.NewCol) INNER JOIN 

								(SELECT Mid(T2_LOOK.[KEY],4,6) AS SuppCode, T2_LOOK.F1 AS MasterSupplier
									FROM T2_LOOK
										WHERE (((Left(T2_LOOK.[KEY],3))='SUP'))
											) as  Suppliers ON T2_HEAD.SUPPLIER = Suppliers.SuppCode) INNER JOIN T2_SIZES ON T2_HEAD.SIZERANGE = T2_SIZES.SIZERANGE) INNER JOIN

											(SELECT Right([T2_LOOK].[KEY],3) AS DeptCode, T2_LOOK.F1 AS MasterDept
												FROM T2_LOOK
													WHERE (Left([T2_LOOK].[KEY],3))='TYP') As Dept ON T2_HEAD.STYPE = Dept.DeptCode) INNER JOIN

								(SELECT Trim(Mid([T2_LOOK]![KEY],4,6)) AS StkType, T2_LOOK.F1 AS MasterStocktype
FROM T2_LOOK
WHERE (((Left([T2_LOOK]![KEY],3))='CAT'))
											ORDER BY Trim(Mid([T2_LOOK].[KEY],4,6))) as Stocktype
									ON T2_HEAD.[GROUP] = Stocktype.StkType) 	LEFT JOIN

									(SELECT Right(T2_LOOK.[KEY],3) AS SubDeptCode, T2_LOOK.F1 AS MasterSubDept
										FROM T2_LOOK
											WHERE (Left(T2_LOOK.[KEY],3))='US2') AS SubDept ON T2_HEAD.USER2 = SubDept.SubDeptCode)
									GROUP BY ([T2_BRA].[REF] + [F7]), Suppliers.MasterSupplier, Dept.MasterDept, Colour.MasterColour, Colour.F7, T2_HEAD.LASTDELV,
									 T2_HEAD.SHORT, T2_HEAD.[DESC], T2_HEAD.[GROUP], T2_HEAD.STYPE, T2_HEAD.SIZERANGE, T2_HEAD.SUPPLIER, T2_HEAD.SUPPREF,
									  T2_HEAD.VAT, T2_HEAD.BASESELL, T2_HEAD.USER1,T2_HEAD.REM, T2_HEAD.REM2,
									 T2_HEAD.SELL, T2_HEAD.SELLB, T2_HEAD.SELL1,T2_HEAD.REF,stocktype.MasterStocktype,SubDept.MasterSubDept,  T2_SIZES.S01, T2_SIZES.S02, T2_SIZES.S03, T2_SIZES.S04, T2_SIZES.S05, 
									 T2_SIZES.S06, T2_SIZES.S07, T2_SIZES.S08, T2_SIZES.S09, T2_SIZES.S10, T2_SIZES.S11, T2_SIZES.S12, T2_SIZES.S13
									ORDER BY ([T2_BRA].[REF] + [F7]) DESC;";

		public static string GetEanCodes => "SELECT * from T2_EAN WHERE EAN_CODE <> NULL";
        
        public static string InsertSKU => @"INSERT INTO [DESC] (SKU) VALUES (?)";

        public static string DoesSKUExist => @"SELECT SKU FROM [DESC] WHERE SKU = (?)";

        public static string DeleteSKUs => @"DELETE FROM [DESC]";

        public static string FetchLatestSeaosn => @"SELECT * FROM [CONFIG]";

		public static string FETCHREF => @"SELECT [T2_BRA].[REF] FROM [T2_BRA]";

		public static string FetchREM => @"SELECT * FROM [REM]";

		public static string SingleSku =>
			@"SELECT ([T2_BRA].[REF] + [F7]) AS NEWSTYLE, Suppliers.MasterSupplier, Dept.MasterDept, Colour.MasterColour, Colour.F7, T2_HEAD.SHORT, T2_HEAD.REM, T2_HEAD.REM2, T2_HEAD.USER1,
	T2_HEAD.[DESC], T2_HEAD.[GROUP], T2_HEAD.STYPE, T2_HEAD.SIZERANGE, T2_HEAD.SUPPLIER, T2_HEAD.SUPPREF, T2_HEAD.VAT, T2_HEAD.BASESELL, T2_HEAD.SELL, T2_HEAD.LASTDELV,
		T2_HEAD.SELLB, T2_HEAD.SELL1, Sum(T2_BRA.Q11) AS QTY1, Sum(T2_BRA.Q12) AS QTY2, Sum(T2_BRA.Q13) AS QTY3, 
			Sum(T2_BRA.Q14) AS QTY4, Sum(T2_BRA.Q15) AS QTY5, Sum(T2_BRA.Q16) AS QTY6, Sum(T2_BRA.Q17) AS QTY7, Sum(T2_BRA.Q18) AS QTY8, 
				Sum(T2_BRA.Q19) AS QTY9, Sum(T2_BRA.Q20) AS QTY10, Sum(T2_BRA.Q21) AS QTY11, Sum(T2_BRA.Q22) AS QTY12, Sum(T2_BRA.Q23) AS QTY13, 
					T2_HEAD.REF,Stocktype.MasterStocktype,SubDept.MasterSubDept,
                        Sum(T2_BRA.LY11) AS LY1, Sum(T2_BRA.LY12) AS LY2, Sum(T2_BRA.LY13) AS LY3, Sum(T2_BRA.LY14) AS LY4, Sum(T2_BRA.LY15) AS LY5, 
                            Sum(T2_BRA.LY16) AS LY6, Sum(T2_BRA.LY17) AS LY7, Sum(T2_BRA.LY18) AS LY8, Sum(T2_BRA.LY19) AS LY9, Sum(T2_BRA.LY20) AS LY10, 
                                Sum(T2_BRA.LY21) AS LY11, Sum(T2_BRA.LY22) AS LY12, Sum(T2_BRA.LY23) AS LY13, T2_HEAD.VAT, T2_SIZES.S01, T2_SIZES.S02, T2_SIZES.S03, T2_SIZES.S04, T2_SIZES.S05, T2_SIZES.S06, T2_SIZES.S07, T2_SIZES.S08, T2_SIZES.S09, T2_SIZES.S10, T2_SIZES.S11, T2_SIZES.S12, T2_SIZES.S13
									FROM (((((((T2_BRA INNER JOIN T2_HEAD ON T2_BRA.REF = T2_HEAD.REF) INNER JOIN (SELECT Right(T2_LOOK.[KEY],3) AS NewCol, T2_LOOK.F1 AS MasterColour, Left(T2_LOOK.[KEY],3) AS Col, T2_LOOK.F7
								FROM T2_LOOK
								WHERE (Left(T2_LOOK.[KEY],3))='COL') as Colour ON T2_BRA.COLOUR = Colour.NewCol) INNER JOIN 

								(SELECT Mid(T2_LOOK.[KEY],4,6) AS SuppCode, T2_LOOK.F1 AS MasterSupplier
									FROM T2_LOOK
										WHERE (((Left(T2_LOOK.[KEY],3))='SUP'))
											) as  Suppliers ON T2_HEAD.SUPPLIER = Suppliers.SuppCode) INNER JOIN T2_SIZES ON T2_HEAD.SIZERANGE = T2_SIZES.SIZERANGE) INNER JOIN

											(SELECT Right([T2_LOOK].[KEY],3) AS DeptCode, T2_LOOK.F1 AS MasterDept
												FROM T2_LOOK
													WHERE (Left([T2_LOOK].[KEY],3))='TYP') As Dept ON T2_HEAD.STYPE = Dept.DeptCode) INNER JOIN

								(SELECT Trim(Mid([T2_LOOK]![KEY],4,6)) AS StkType, T2_LOOK.F1 AS MasterStocktype
FROM T2_LOOK
WHERE (((Left([T2_LOOK]![KEY],3))='CAT'))
											ORDER BY Trim(Mid([T2_LOOK].[KEY],4,6))) as Stocktype
									ON T2_HEAD.[GROUP] = Stocktype.StkType) 	LEFT JOIN

									(SELECT Right(T2_LOOK.[KEY],3) AS SubDeptCode, T2_LOOK.F1 AS MasterSubDept
										FROM T2_LOOK
											WHERE (Left(T2_LOOK.[KEY],3))='US2') AS SubDept ON T2_HEAD.USER2 = SubDept.SubDeptCode)
                                    WHERE [T2_BRA].[REF] = ? 
									GROUP BY ([T2_BRA].[REF] + [F7]), Suppliers.MasterSupplier, Dept.MasterDept, Colour.MasterColour, Colour.F7, T2_HEAD.LASTDELV, T2_HEAD.USER1,
									 T2_HEAD.SHORT, T2_HEAD.[DESC], T2_HEAD.[GROUP], T2_HEAD.STYPE, T2_HEAD.SIZERANGE, T2_HEAD.SUPPLIER, T2_HEAD.SUPPREF, T2_HEAD.REM, T2_HEAD.REM2,
									  T2_HEAD.VAT, T2_HEAD.BASESELL,
									 T2_HEAD.SELL, T2_HEAD.SELLB, T2_HEAD.SELL1,T2_HEAD.REF,stocktype.MasterStocktype,SubDept.MasterSubDept,  T2_SIZES.S01, T2_SIZES.S02, T2_SIZES.S03, T2_SIZES.S04, T2_SIZES.S05, 
									 T2_SIZES.S06, T2_SIZES.S07, T2_SIZES.S08, T2_SIZES.S09, T2_SIZES.S10, T2_SIZES.S11, T2_SIZES.S12, T2_SIZES.S13
									ORDER BY ([T2_BRA].[REF] + [F7]) DESC;";
	}
}
