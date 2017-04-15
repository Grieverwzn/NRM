using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using NPOI.SS.UserModel;
using NPOI.HSSF.UserModel;
using System.Windows.Forms;
using NPOI.SS.Util;
using NPOI.HSSF.Util;


namespace com.foxmail.wyyuan1991.Common.ExcelHelper
{
   public class ExcelHelperV2
    {
       /// <summary>
       /// 根据行列树的节点集合生成相应的Excel集合
       /// </summary>
       /// <param name="sheetName">当前要生成的表名</param>
       /// <param name="rowNodeCollection">行项目集合</param>
       /// <param name="columNodeCollection">列项目集合</param>
       /// <param name="sheetHeadernName">当前Excel表的表头</param>
       public static void ExportIndexListByTreeNodeCollection(List<TreeNode> rowNodeCollection,List<TreeNode> columNodeCollection,string sheetName,string sheetHeadernName)
        {
            //表头所占用的行数
            int headerRowNum = 0;

            //表头所占用的列数
            int headerColumnNum = 0;

            //所有指标项所占用的单元格
            int allIndexItemNum = 0;

            //绘制单元格当前的列号
            int currentColumnNum = 0;

            //绘制单元格当前的行号
            int currentRowNum = 0;

            workbook = new HSSFWorkbook();

            HSSFSheet sheet = (HSSFSheet)workbook.CreateSheet(sheetName);

            HSSFFont font = (HSSFFont)workbook.CreateFont();

            font.FontName = "微软雅黑";

            font.FontHeightInPoints = 12;

            HSSFFont bigFont = (HSSFFont)workbook.CreateFont();

            bigFont.FontName = "微软雅黑";

            bigFont.FontHeightInPoints = 18;

            //带有颜色的格式
            HSSFCellStyle bigStyle = (HSSFCellStyle)workbook.CreateCellStyle();

            bigStyle.SetFont(bigFont);

            bigStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;

            bigStyle.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;

            bigStyle.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;

            bigStyle.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;

            bigStyle.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;

            bigStyle.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;

            //不带颜色的格式
            HSSFCellStyle normalStyle = (HSSFCellStyle)workbook.CreateCellStyle();

            normalStyle.SetFont(font);

            normalStyle.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Center;

            normalStyle.VerticalAlignment = NPOI.SS.UserModel.VerticalAlignment.Center;

            normalStyle.BorderBottom = NPOI.SS.UserModel.BorderStyle.Thin;

            normalStyle.BorderLeft = NPOI.SS.UserModel.BorderStyle.Thin;

            normalStyle.BorderRight = NPOI.SS.UserModel.BorderStyle.Thin;

            normalStyle.BorderTop = NPOI.SS.UserModel.BorderStyle.Thin;

            //获得当前表头项最底层深度
            headerRowNum = MyNodeLevels(rowNodeCollection);

            //获得所有表头所加起来的宽度
            headerColumnNum = GetAllIndexWidth(rowNodeCollection);

            //在对单元格赋值或者使用前必须先创建单元格,先创建当前表头包含的所有单元格
            for (int i = 0; i <=headerRowNum; i++)
            {
                IRow row = sheet.CreateRow(i);

                for (int n = 0; n < headerColumnNum; n++)
                {
                    row.CreateCell(n);
                }
            }
            
           //绘制Excel表标题
            sheet.AddMergedRegion(new CellRangeAddress(0, 0, 0, headerColumnNum - 1));

            sheet.SetEnclosedBorderOfRegion(new CellRangeAddress(0, 0, 0, headerColumnNum - 1), NPOI.SS.UserModel.BorderStyle.Thin, HSSFColor.Black.Index);

            //给当前单元格赋值
            sheet.GetRow(0).GetCell(0).SetCellValue(sheetHeadernName);

            sheet.GetRow(0).GetCell(0).CellStyle = bigStyle;

            //绘制Excel表头单元格
            foreach (TreeNode node in rowNodeCollection)
            {
                DrawIndexHeaderToExcel(node,ref sheet, currentColumnNum,headerRowNum);

                currentColumnNum += GetIndexHeaderWidth(node);
            }

            //根据列项目进行Excel的绘制，首先获取所有指标项所占用的行数

            allIndexItemNum = GetAllIndexWidth(columNodeCollection);

            //在对单元格赋值或者使用前必须先创建单元格,先创建当前所有指标项所包含的单元格
            for (int i = headerRowNum + 1; i <= headerRowNum+allIndexItemNum; i++)
            {
                IRow row = sheet.CreateRow(i);

                for (int n = 0; n < headerColumnNum; n++)
                {
                    row.CreateCell(n);
                }
            }
            currentRowNum = headerRowNum+1;

            foreach (TreeNode node in columNodeCollection)
            {
                DrawIndexItemToExcel(node,ref sheet,currentRowNum);

                currentRowNum += GetIndexHeaderWidth(node);
            }

            //进行单元格格式处理
            for (int i = 1; i <=headerRowNum + allIndexItemNum; i++)
            {
                IRow row = sheet.GetRow(i);

                for (int n = 0; n < headerColumnNum; n++)
                {
                    ICell cell = row.GetCell(n);

                    cell.CellStyle = normalStyle;
                }
            }

            WriteToFile();
        }

        private static void DrawIndexHeaderToExcel(TreeNode node, ref  HSSFSheet sheet, int currentColumnNum,int rowHeaderNum)
        {
            Queue<TreeNode> currrentTreeNodeQueue = new Queue<TreeNode>();

            //首先给当前指标所在的列号赋值

            //把当前指标项的列号赋给第一个节点
            node.Tag= currentColumnNum;

            //先把当前父节点排入队列
            currrentTreeNodeQueue.Enqueue(node);
 
            while (currrentTreeNodeQueue.Count>0)
            {
                TreeNode currrentNode = currrentTreeNodeQueue.Dequeue();

                int currentIndexColumnNum = (int) currrentNode.Tag;

                //获得当前节点的子节点所占用的单元格数量
                int currentNodeWidth = GetIndexHeaderWidth(currrentNode);

                //若当前节点没有子节点
                if (currrentNode.Nodes.Count == 0)
                {
                    //有必要合并单元格，并设置单元格格式
                    sheet.AddMergedRegion(new CellRangeAddress(currrentNode.Level, rowHeaderNum, currentIndexColumnNum, currentIndexColumnNum + currentNodeWidth-1));

                    sheet.SetEnclosedBorderOfRegion(new CellRangeAddress(currrentNode.Level, rowHeaderNum, currentIndexColumnNum, currentIndexColumnNum + currentNodeWidth-1), NPOI.SS.UserModel.BorderStyle.Thin, HSSFColor.Black.Index);

                }
                else
                {
                    //有必要合并单元格，并设置单元格格式
                    sheet.AddMergedRegion(new CellRangeAddress(currrentNode.Level, currrentNode.Level, currentIndexColumnNum, currentIndexColumnNum + currentNodeWidth-1));

                    sheet.SetEnclosedBorderOfRegion(new CellRangeAddress(currrentNode.Level, currrentNode.Level, currentIndexColumnNum, currentIndexColumnNum + currentNodeWidth-1), NPOI.SS.UserModel.BorderStyle.Thin, HSSFColor.Black.Index);
                }

                //给当前单元格赋值
                sheet.GetRow(currrentNode.Level).GetCell(currentIndexColumnNum).SetCellValue(currrentNode.Text);

                int currentColumnWidth0 = sheet.GetColumnWidth(currentIndexColumnNum);

                currentColumnWidth0 = currentColumnWidth0 > (currrentNode.Text.Length + 10) * 256 ? currentColumnWidth0 : (currrentNode.Text.Length + 10) * 256;

                sheet.SetColumnWidth(currentIndexColumnNum, currentColumnWidth0);

                //当前节点包含子节点
                if (currrentNode.Nodes.Count > 0)
                {   
                    //把当前父节点的列号赋给字节点
                    int currentChildNodeColumnNum =(int)currrentNode.Tag;

                    for (int i = 0; i < currrentNode.Nodes.Count; i++)
                    {
                        if (i == 0)
                        {
                            currrentNode.Nodes[i].Tag = currrentNode.Tag;
                        }
                        else
                        {
                            currentChildNodeColumnNum += GetIndexHeaderWidth(currrentNode.Nodes[i-1]);

                            currrentNode.Nodes[i].Tag = currentChildNodeColumnNum;
                        }
                        currrentTreeNodeQueue.Enqueue(currrentNode.Nodes[i]);
                    }
                }
            }
        }

        private static void DrawIndexItemToExcel(TreeNode node, ref  HSSFSheet sheet, int currentRowNum)
       {
           Queue<TreeNode> currrentTreeNodeQueue = new Queue<TreeNode>();

            int currentIndexRowNum = currentRowNum;

           //首先给当前指标所在的行号赋值
            Type type = node.Tag!=null?node.Tag.GetType():null;

            //如果是List<double>类型，则说明是最底层指标直接进行绘制即可，不再为Tag赋值
            if (type==null||type!= typeof(List<double>))
            {
                node.Tag = currentRowNum;
            }

           //先把当前父节点排入队列
           currrentTreeNodeQueue.Enqueue(node);

           while (currrentTreeNodeQueue.Count > 0)
           {
               TreeNode currrentNode = currrentTreeNodeQueue.Dequeue();

               type = currrentNode.Tag.GetType();

               //若当前节点的Tag值不为List<double>，则继续进行合并单元格和赋值
               if (type == typeof (List<double>))
               {
                   List<double> indexValueList = (List<double>) currrentNode.Tag;

                   if (currrentNode.PrevNode != null)
                   {
                       currrentNode.Tag = (int) currrentNode.PrevNode.Tag + 1;
                   }
                   else
                   {
                       currrentNode.Tag = currentIndexRowNum;
                   }
                   //给当前单元格赋值
                   sheet.GetRow((int)currrentNode.Tag).GetCell(currrentNode.Level-1).SetCellValue(currrentNode.Text);

                   int currentColumnWidth = sheet.GetColumnWidth(currrentNode.Level - 1);

                   currentColumnWidth= currentColumnWidth > (currrentNode.Text.Length + 10) * 256 ? currentColumnWidth : (currrentNode.Text.Length + 10) * 256;

                   sheet.SetColumnWidth(currrentNode.Level - 1, currentColumnWidth);

                   for (int i = 0; i < indexValueList.Count; i++)
                   {
                       //给当前单元格赋值
                       ICell cell = sheet.GetRow((int)currrentNode.Tag).GetCell(currrentNode.Level+i);

                       SetCellValue(cell,indexValueList[i]);

                       int currentColumnWidth0 = sheet.GetColumnWidth(currrentNode.Level + i);

                       currentColumnWidth0 = currentColumnWidth0 > (currrentNode.Text.Length + 10) * 256 ? currentColumnWidth0 : (currrentNode.Text.Length + 10) * 256;

                       sheet.SetColumnWidth(currrentNode.Level + i, currentColumnWidth0);
                   }  
               }
               else
               {
                   currentIndexRowNum = (int)currrentNode.Tag;

                   //获得当前节点的子节点所占用的单元格数量
                   int currentNodeWidth = GetIndexHeaderWidth(currrentNode);

                   //若当前还有子节点
                   if (currrentNode.Nodes.Count > 0)
                   {
                       //有必要合并单元格，并设置单元格格式
                       sheet.AddMergedRegion(new CellRangeAddress((int)currrentNode.Tag, (int)currrentNode.Tag+currentNodeWidth-1,currrentNode.Level-1, currrentNode.Level - 1));

                       sheet.SetEnclosedBorderOfRegion(new CellRangeAddress((int)currrentNode.Tag, (int)currrentNode.Tag+currentNodeWidth-1,currrentNode.Level-1, currrentNode.Level - 1), NPOI.SS.UserModel.BorderStyle.Thin, HSSFColor.Black.Index);
                   }

                   //给当前单元格赋值
                   sheet.GetRow((int)currrentNode.Tag).GetCell(currrentNode.Level-1).SetCellValue(currrentNode.Text);

                   int currentColumnWidth0 = sheet.GetColumnWidth(currrentNode.Level - 1);

                   currentColumnWidth0 = currentColumnWidth0 > (currrentNode.Text.Length + 10) * 256 ? currentColumnWidth0 : (currrentNode.Text.Length + 10) * 256;

                   sheet.SetColumnWidth(currrentNode.Level - 1, currentColumnWidth0);
               }

               //当前节点包含子节点
               if (currrentNode.Nodes.Count > 0)
               {
                   if (currrentNode.Nodes[0].Tag.GetType() != typeof (List<double>))
                   {
                       //把当前父节点的列号赋给子节点
                       int currentChildNodeRowNum = (int) currrentNode.Tag;

                       for (int i = 0; i < currrentNode.Nodes.Count; i++)
                       {
                           if (i == 0)
                           {
                               currrentNode.Nodes[i].Tag = currrentNode.Tag;
                           }
                           else
                           {
                               currentChildNodeRowNum += GetIndexHeaderWidth(currrentNode.Nodes[i - 1]);

                               currrentNode.Nodes[i].Tag = currentChildNodeRowNum;
                           }
                           currrentTreeNodeQueue.Enqueue(currrentNode.Nodes[i]);
                       }
                   }
                   else
                   {
                       for (int i = 0; i < currrentNode.Nodes.Count; i++)
                       {
                           currrentTreeNodeQueue.Enqueue(currrentNode.Nodes[i]);
                       }
                   }
               }
           }
       }
        private static int iNodeLevels = 0;

        /// <summary>
        /// 调用递归求最大层数
        /// </summary>
        private static int MyNodeLevels(List<TreeNode> treeNodeCollection)
        {
            iNodeLevels = 1;//初始為1

            int iNodeDeep = MyGetNodeLevels(treeNodeCollection);

            return iNodeDeep;
        }

        /// <summary>
        /// 递归计算树最大层数
        /// </summary>
        /// <param name="tnc"></param>
        /// <returns></returns>
        private static int MyGetNodeLevels(List<TreeNode> tnc)
        {
            if (tnc == null) return 0;

            foreach (TreeNode tn in tnc)
            {
                if (tn.Level > iNodeLevels)//tn.Level是從0開始的
                {
                    iNodeLevels = tn.Level;
                }
                if (tn.Nodes.Count > 0)
                {
                    MyGetNodeLevels(tn.Nodes.Cast<TreeNode>().ToList());
                }
            }

            return iNodeLevels;
        }


        private static int GetAllIndexWidth(List<TreeNode> indexNodeList)
        {
            int totalCellNum = indexNodeList.Sum(indexNode => GetIndexHeaderWidth(indexNode));

            return totalCellNum;
        }
        /// <summary>
        /// 获得合并标题字段所占的单元格数量
        /// </summary>
        /// <param name="node">节点</param>
        /// <returns>单元格数量</returns>
        private static int GetIndexHeaderWidth(TreeNode node)
        {
            int uhCellNum = 0;

            if (node.Nodes.Count == 0)
            {
                return 1;
            }

            //获得最底层字段的宽度
            for (int i = 0; i <node.Nodes.Count; i++)
            {
                uhCellNum = uhCellNum + GetIndexHeaderWidth(node.Nodes[i]);
            }
            return uhCellNum;
        }

        static HSSFWorkbook workbook;

        public static void WriteToFile()
        {
            //Write the stream data of workbook to the root directory
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.DefaultExt = "xls";
            saveFileDialog.Filter = "Excel 文件|*.xls*";
            string saveFileName = "";
            saveFileDialog.FileName = saveFileName;
            MemoryStream ms = new MemoryStream();
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                saveFileName = saveFileDialog.FileName;
                if (!CheckFiles(saveFileName))
                {
                    MessageBox.Show("文件被占用，请关闭文件 " + saveFileName);
                    workbook = null;
                    ms.Close();
                    ms.Dispose();
                    return;
                }
                workbook.Write(ms);

                FileStream file = new FileStream(saveFileName, FileMode.Create);

                workbook.Write(file);

                file.Close();

                MessageBox.Show("文件生成成功！");
            }
        }

        /// <summary>
        /// 根据数据类型设置不同类型的cell，写Excel有用
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="obj"></param>
        public static void SetCellValue(ICell cell, object obj)
        {
            if (obj is int)
            {
                cell.SetCellValue((int)obj);
            }
            else if (obj is double)
            {
                cell.SetCellValue((double)obj);
            }
            else if (obj.GetType() == typeof(IRichTextString))
            {
                cell.SetCellValue((IRichTextString)obj);
            }
            else if (obj is string)
            {
                cell.SetCellValue(obj.ToString());
            }
            else if (obj is DateTime)
            {
                cell.SetCellValue((DateTime)obj);
            }
            else if (obj is bool)
            {
                cell.SetCellValue((bool)obj);
            }
            else
            {
                cell.SetCellValue(obj.ToString());
            }
        }

        # region 检测文件占用
        [DllImport("kernel32.dll")]
        public static extern IntPtr _lopen(string lpPathName, int iReadWrite);
        [DllImport("kernel32.dll")]
        public static extern bool CloseHandle(IntPtr hObject);
        public const int OF_READWRITE = 2;
        public const int OF_SHARE_DENY_NONE = 0x40;
        public static readonly IntPtr HFILE_ERROR = new IntPtr(-1);
        /// <summary>
        /// 检测文件被占用 
        /// </summary>
        /// <param name="FileNames">要检测的文件路径</param>
        /// <returns></returns>
        public static bool CheckFiles(string FileNames)
        {
            if (!File.Exists(FileNames))
            {
                //文件不存在
                return true;
            }
            IntPtr vHandle = _lopen(FileNames, OF_READWRITE | OF_SHARE_DENY_NONE);
            if (vHandle == HFILE_ERROR)
            {
                //文件被占用
                return false;
            }
            //文件没被占用
            CloseHandle(vHandle);
            return true;
        }
        #endregion
    }
}
