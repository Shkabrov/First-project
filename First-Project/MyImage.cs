using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Drawing.Drawing2D;

namespace ImageLib
{
    public class MyImage
    {
        //image
        Image image;

        [JsonProperty]
        string imageHex;

        [JsonIgnore]
        public Image Image
        {
            get
            {
                if (image == null)
                    image = HexToImage(imageHex);
                return image;
            }
        }

        //List<obj>
        [JsonProperty]
        List<ImgData> infList = new List<ImgData>();

        Form _form;

        Panel toolPnl;
        Panel pnlClrB;
        Panel pnlWdthB;
        Panel pnlDshB;
        Panel pnlClrO;
        Panel pnlShpB;
        Panel pnlTrnsp;

        Button btnCrt;

        #region picture box
        PictureBox _pictureBox;

        public void LoadControls(Form form, PictureBox pictureBox, DataGridView dataGridView)
        {
            _form = form;
            _form.KeyDown += form_KeyDown;
            _form.KeyPreview = true;

            SetPictureBox(pictureBox);
            SetDataGridView(dataGridView);
            CreateButton();
        }

        public void ReleaseObjects()
        {
            //Form
            _form.KeyDown -= form_KeyDown;

            //PictureBox
            _pictureBox.MouseDown -= pictureBox_MouseDown;
            _pictureBox.MouseMove -= pictureBox_MouseMove;
            _pictureBox.MouseUp -= pictureBox_MouseUp;
            _pictureBox.Paint -= pictureBox_Paint;
            _pictureBox.MouseEnter -= pictureBox_MouseEnter;
            _pictureBox.MouseLeave -= pictureBox_MouseLeave;

            //DataGridView
            _dataGridView.CellEndEdit -= dataGridView_CellEndEdit;
            _dataGridView.CellClick -= dataGridView_CellClick;

            _dataGridView.Columns.Clear();
            if (toolPnl.Visible)
                HideToolPanel();

            toolPnl.Dispose();
            btnCrt.Dispose();
            ImgData.CountID = 0;
        }

        void SetPictureBox(PictureBox pictureBox)
        {
            _pictureBox = pictureBox;

            //Растягивание изображения
            Bitmap newImage = new Bitmap(pictureBox.Width, pictureBox.Height);
            using (Graphics gr = Graphics.FromImage(newImage))
                gr.DrawImage(image, new Rectangle(0, 0, pictureBox.Width, pictureBox.Height));

            image = newImage;

            _pictureBox.Image = image;

            _pictureBox.MouseDown += pictureBox_MouseDown;
            _pictureBox.MouseMove += pictureBox_MouseMove;
            _pictureBox.MouseUp += pictureBox_MouseUp;
            _pictureBox.Paint += pictureBox_Paint;
            _pictureBox.MouseEnter += pictureBox_MouseEnter;
            _pictureBox.MouseLeave += pictureBox_MouseLeave;

            CreatePanel();
        }

        #region События pictureBox и связанные методы

        ImgData selectObj;
        ImgData findObj;

        GraphicsPath myPath;

        Bitmap bitmapImage;

        Point lastPosition;

        Rectangle screen = Cursor.Clip;

        readonly int dfltSz = 30;

        bool normalMode = true;

        bool redrawingObjs = true;
        bool drawingBrdObjs;
        bool drawingLighting;

        bool borderCreating;
        bool mouseInSlctObj;

        bool tempObjDrawing;

        Rectangle draggedRect;
        int deltaX = 0;
        int deltaY = 0;

        Dictionary<string, bool> borderChMsIn = new Dictionary<string, bool>
        {
            { "outlineBoxLftBot", false },
            { "outlineBoxLftMid", false },
            { "outlineBoxLftTop", false },
            { "outlineBoxTopMid", false },
            { "outlineBoxRghtTop", false },
            { "outlineBoxRghtMid", false },
            { "outlineBoxRghtBot", false },
            { "outlineBoxBotMid", false }
        };

        Rectangle outlineBoxLftBot;
        Rectangle outlineBoxLftMid;
        Rectangle outlineBoxLftTop;
        Rectangle outlineBoxTopMid;
        Rectangle outlineBoxRghtTop;
        Rectangle outlineBoxRghtMid;
        Rectangle outlineBoxRghtBot;
        Rectangle outlineBoxBotMid;

        private void form_KeyDown(object sender, KeyEventArgs e)
        {
            #region Преждевременное прерывание перемещения и изменения размеров объекта
            if (!normalMode && mouseInSlctObj && e.KeyCode == Keys.Escape
                || !normalMode && borderChMsIn.ContainsValue(true) && e.KeyCode == Keys.Escape)
            {
                normalMode = true;
                drawingBrdObjs = true;

                Cursor.Clip = screen;

                ShowToolPanel();

                _pictureBox.Invalidate();
            }
            #endregion
        }

        private void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            #region Начало перемещения объекта
            if (mouseInSlctObj)
            {
                normalMode = false;

                deltaX = e.X - selectObj.Rectangle.X;
                deltaY = e.Y - selectObj.Rectangle.Y;

                draggedRect = new Rectangle(selectObj.Rectangle.Location, selectObj.Rectangle.Size);

                Point locationPB = _pictureBox.PointToScreen(new Point(0, 0));

                locationPB.X += deltaX;
                locationPB.Y += deltaY;

                Size sizePB = _pictureBox.Size;

                sizePB.Width -= selectObj.Rectangle.Width;
                sizePB.Height -= selectObj.Rectangle.Height;

                if (sizePB.Width < 1)
                    sizePB.Width = 1;

                if (sizePB.Height < 1)
                    sizePB.Height = 1;

                Cursor.Clip = new Rectangle(locationPB, sizePB);

                if (toolPnl.Visible)
                    HideToolPanel();
            }
            #endregion

            #region Начало изменения размеров объекта
            if (borderChMsIn.ContainsValue(true))
            {
                normalMode = false;

                draggedRect = new Rectangle(selectObj.Rectangle.Location, selectObj.Rectangle.Size);

                if (borderChMsIn["outlineBoxLftBot"] == true)
                {
                    Point locationField = _pictureBox.PointToScreen(new Point(0, draggedRect.Y + dfltSz));

                    Size sizeField = new Size(draggedRect.X + draggedRect.Width - dfltSz,
                        _pictureBox.Height - draggedRect.Y - dfltSz);

                    Cursor.Clip = new Rectangle(locationField, sizeField);
                }
                else if (borderChMsIn["outlineBoxLftMid"] == true)
                {
                    Point locationField = _pictureBox.PointToScreen(new Point(0, e.Y));

                    Size sizeField = new Size(draggedRect.X + draggedRect.Width - dfltSz, 1);

                    Cursor.Clip = new Rectangle(locationField, sizeField);
                }
                else if (borderChMsIn["outlineBoxLftTop"] == true)
                {
                    Point locationField = _pictureBox.PointToScreen(new Point(0, 0));

                    Size sizeField = new Size(draggedRect.X + draggedRect.Width - dfltSz,
                        draggedRect.Y + draggedRect.Height - dfltSz);

                    Cursor.Clip = new Rectangle(locationField, sizeField);
                }
                else if (borderChMsIn["outlineBoxTopMid"] == true)
                {
                    Point locationField = _pictureBox.PointToScreen(new Point(e.X, 0));

                    Size sizeField = new Size(1, draggedRect.Y + draggedRect.Height - dfltSz);

                    Cursor.Clip = new Rectangle(locationField, sizeField);
                }
                else if (borderChMsIn["outlineBoxRghtTop"] == true)
                {
                    Point locationField = _pictureBox.PointToScreen(new Point(draggedRect.X + dfltSz, 0));

                    Size sizeField = new Size(_pictureBox.Width - draggedRect.X - dfltSz,
                        draggedRect.Y + draggedRect.Height - dfltSz);

                    Cursor.Clip = new Rectangle(locationField, sizeField);
                }
                else if (borderChMsIn["outlineBoxRghtMid"] == true)
                {
                    Point locationField = _pictureBox.PointToScreen(new Point(draggedRect.X + dfltSz, e.Y));

                    Size sizeField = new Size(_pictureBox.Width - draggedRect.X - dfltSz, 1);

                    Cursor.Clip = new Rectangle(locationField, sizeField);
                }
                else if (borderChMsIn["outlineBoxRghtBot"] == true)
                {
                    Point locationField = _pictureBox.PointToScreen(new Point(draggedRect.X + dfltSz, draggedRect.Y + dfltSz));

                    Size sizeField = new Size(_pictureBox.Width - draggedRect.X - dfltSz,
                        _pictureBox.Height - draggedRect.Y - dfltSz);

                    Cursor.Clip = new Rectangle(locationField, sizeField);
                }
                else if (borderChMsIn["outlineBoxBotMid"] == true)
                {
                    Point locationField = _pictureBox.PointToScreen(new Point(e.X, draggedRect.Y + dfltSz));

                    Size sizeField = new Size(1, _pictureBox.Height - draggedRect.Y - dfltSz);

                    Cursor.Clip = new Rectangle(locationField, sizeField);
                }

                if (toolPnl.Visible)
                    HideToolPanel();
            }
            #endregion
        }

        private void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            #region Нахождение курсора мыши внутри объекта
            if (normalMode)
            {
                ImgData tempObj = ContainObject(e.Location);

                #region Нахождение курсора мыши внутри точек на рамке
                if (selectObj != null && selectObj.BorderWidth != 0)
                {
                    if (outlineBoxLftBot.Contains(e.Location))
                    {
                        _pictureBox.Cursor = Cursors.SizeNESW;
                        borderChMsIn["outlineBoxLftBot"] = true;
                    }

                    else if (outlineBoxLftMid.Contains(e.Location))
                    {
                        _pictureBox.Cursor = Cursors.SizeWE;
                        borderChMsIn["outlineBoxLftMid"] = true;
                    }

                    else if (outlineBoxLftTop.Contains(e.Location))
                    {
                        _pictureBox.Cursor = Cursors.SizeNWSE;
                        borderChMsIn["outlineBoxLftTop"] = true;
                    }

                    else if (outlineBoxTopMid.Contains(e.Location))
                    {
                        _pictureBox.Cursor = Cursors.SizeNS;
                        borderChMsIn["outlineBoxTopMid"] = true;
                    }

                    else if (outlineBoxRghtTop.Contains(e.Location))
                    {
                        _pictureBox.Cursor = Cursors.SizeNESW;
                        borderChMsIn["outlineBoxRghtTop"] = true;
                    }

                    else if (outlineBoxRghtMid.Contains(e.Location))
                    {
                        _pictureBox.Cursor = Cursors.SizeWE;
                        borderChMsIn["outlineBoxRghtMid"] = true;
                    }

                    else if (outlineBoxRghtBot.Contains(e.Location))
                    {
                        _pictureBox.Cursor = Cursors.SizeNWSE;
                        borderChMsIn["outlineBoxRghtBot"] = true;
                    }

                    else if (outlineBoxBotMid.Contains(e.Location))
                    {
                        _pictureBox.Cursor = Cursors.SizeNS;
                        borderChMsIn["outlineBoxBotMid"] = true;
                    }
                    else
                    {
                        borderChMsIn["outlineBoxLftBot"] = false;
                        borderChMsIn["outlineBoxLftMid"] = false;
                        borderChMsIn["outlineBoxLftTop"] = false;
                        borderChMsIn["outlineBoxTopMid"] = false;
                        borderChMsIn["outlineBoxRghtTop"] = false;
                        borderChMsIn["outlineBoxRghtMid"] = false;
                        borderChMsIn["outlineBoxRghtBot"] = false;
                        borderChMsIn["outlineBoxBotMid"] = false;
                    }
                }

                if (borderChMsIn.ContainsValue(true))
                    if (mouseInSlctObj)
                        mouseInSlctObj = false;
                #endregion

                if (!borderChMsIn.ContainsValue(true))
                {
                    findObj = tempObj;

                    #region Нахождение курсора мыши внутри не выделенного объекта
                    if (findObj != selectObj && findObj != null)
                    {
                        _pictureBox.Cursor = Cursors.Hand;

                        if (selectObj != null)
                            drawingBrdObjs = true;
                        else
                            redrawingObjs = true;


                        drawingLighting = true;

                        _pictureBox.Invalidate();
                    }
                    else
                    {
                        if (_pictureBox.Cursor != Cursors.Default)
                        {
                            _pictureBox.Cursor = Cursors.Default;

                            if (selectObj != null)
                                drawingBrdObjs = true;
                            else
                                redrawingObjs = true;

                            if (mouseInSlctObj)
                                mouseInSlctObj = false;

                            _pictureBox.Invalidate();
                        }
                    }
                    #endregion

                    #region Нахождение курсора мыши внутри выделенного объекта
                    if (findObj == selectObj && selectObj != null)
                    {
                        _pictureBox.Cursor = Cursors.SizeAll;
                        mouseInSlctObj = true;
                    }
                    #endregion
                }
            }
            #endregion

            #region Перемещение объекта
            if (!normalMode && mouseInSlctObj && e.Button == MouseButtons.Left)
            {
                tempObjDrawing = true;

                if (!IntersectionObjectMv(e.Location))
                {
                    lastPosition = Cursor.Position;

                    draggedRect.X = e.X - deltaX;
                    draggedRect.Y = e.Y - deltaY;
                }
                else
                    Cursor.Position = lastPosition;

                _pictureBox.Invalidate();
            }
            #endregion

            #region Изменение размеров объекта
            if (!normalMode && borderChMsIn.ContainsValue(true) && e.Button == MouseButtons.Left)
            {
                tempObjDrawing = true;

                if (borderChMsIn["outlineBoxLftBot"] == true)
                {
                    Rectangle newRect = new Rectangle(e.X, draggedRect.Y, draggedRect.Width + draggedRect.X - e.X,
                        draggedRect.Height + e.Y - draggedRect.Y - draggedRect.Height);

                    if (!IntersectionObjectChg(newRect))
                    {
                        lastPosition = Cursor.Position;

                        draggedRect.Width += draggedRect.X - e.X;
                        draggedRect.X = e.X;

                        draggedRect.Height += e.Y - draggedRect.Y - draggedRect.Height;
                    }
                    else
                        Cursor.Position = lastPosition;
                }
                else if (borderChMsIn["outlineBoxLftMid"] == true)
                {
                    Rectangle newRect = new Rectangle(e.X, draggedRect.Y, draggedRect.Width + draggedRect.X - e.X,
                        draggedRect.Height);

                    if (!IntersectionObjectChg(newRect))
                    {
                        lastPosition = Cursor.Position;

                        draggedRect.Width += draggedRect.X - e.X;
                        draggedRect.X = e.X;
                    }
                    else
                        Cursor.Position = lastPosition;
                }
                else if (borderChMsIn["outlineBoxLftTop"] == true)
                {
                    Rectangle newRect = new Rectangle(e.X, e.Y, draggedRect.Width + draggedRect.X - e.X,
                        draggedRect.Height + draggedRect.Y - e.Y);

                    if (!IntersectionObjectChg(newRect))
                    {
                        lastPosition = Cursor.Position;

                        draggedRect.Width += draggedRect.X - e.X;
                        draggedRect.X = e.X;

                        draggedRect.Height += draggedRect.Y - e.Y;
                        draggedRect.Y = e.Y;
                    }
                    else
                        Cursor.Position = lastPosition;
                }
                else if (borderChMsIn["outlineBoxTopMid"] == true)
                {
                    Rectangle newRect = new Rectangle(draggedRect.X, e.Y, draggedRect.Width,
                        draggedRect.Height + draggedRect.Y - e.Y);

                    if (!IntersectionObjectChg(newRect))
                    {
                        lastPosition = Cursor.Position;

                        draggedRect.Height += draggedRect.Y - e.Y;
                        draggedRect.Y = e.Y;
                    }
                    else
                        Cursor.Position = lastPosition;
                }
                else if (borderChMsIn["outlineBoxRghtTop"] == true)
                {
                    Rectangle newRect = new Rectangle(draggedRect.X, e.Y, draggedRect.Width + e.X - draggedRect.X - draggedRect.Width,
                        draggedRect.Height + draggedRect.Y - e.Y);

                    if (!IntersectionObjectChg(newRect))
                    {
                        lastPosition = Cursor.Position;

                        draggedRect.Width += e.X - draggedRect.X - draggedRect.Width;

                        draggedRect.Height += draggedRect.Y - e.Y;
                        draggedRect.Y = e.Y;
                    }
                    else
                        Cursor.Position = lastPosition;
                }
                else if (borderChMsIn["outlineBoxRghtMid"] == true)
                {
                    Rectangle newRect = new Rectangle(draggedRect.X, draggedRect.Y,
                        draggedRect.Width + e.X - draggedRect.X - draggedRect.Width, draggedRect.Height);

                    if (!IntersectionObjectChg(newRect))
                    {
                        lastPosition = Cursor.Position;

                        draggedRect.Width += e.X - draggedRect.X - draggedRect.Width;
                    }
                    else
                        Cursor.Position = lastPosition;
                }
                else if (borderChMsIn["outlineBoxRghtBot"] == true)
                {
                    Rectangle newRect = new Rectangle(draggedRect.X, draggedRect.Y,
                        draggedRect.Width + e.X - draggedRect.X - draggedRect.Width,
                        draggedRect.Height + e.Y - draggedRect.Y - draggedRect.Height);

                    if (!IntersectionObjectChg(newRect))
                    {
                        lastPosition = Cursor.Position;

                        draggedRect.Width += e.X - draggedRect.X - draggedRect.Width;

                        draggedRect.Height += e.Y - draggedRect.Y - draggedRect.Height;
                    }
                    else
                        Cursor.Position = lastPosition;
                }
                else if (borderChMsIn["outlineBoxBotMid"] == true)
                {
                    Rectangle newRect = new Rectangle(draggedRect.X, draggedRect.Y, draggedRect.Width,
                        draggedRect.Height + e.Y - draggedRect.Y - draggedRect.Height);

                    if (!IntersectionObjectChg(newRect))
                    {
                        lastPosition = Cursor.Position;

                        draggedRect.Height += e.Y - draggedRect.Y - draggedRect.Height;
                    }
                    else
                        Cursor.Position = lastPosition;
                }
                _pictureBox.Invalidate();
            }
            #endregion
        }

        private void pictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            #region Нажатие на объект
            if (normalMode && findObj != selectObj && findObj != null)
            {
                drawingBrdObjs = true;
                mouseInSlctObj = true;

                _pictureBox.Cursor = Cursors.SizeAll;

                selectObj = findObj;

                //Отмена выделения
                _dataGridView.ClearSelection();

                //Выделение найденных объектов в dataGridView`е
                foreach (DataGridViewRow row in _dataGridView.Rows)
                {
                    if (row.Cells[0].Value.Equals(selectObj.ID))
                    {
                        _dataGridView.CurrentCell = _dataGridView.Rows[row.Index].Cells[1];
                        _dataGridView.Rows[row.Index].Selected = true;
                        break;
                    }
                }

                myPath = new GraphicsPath();

                switch (selectObj.Shape)
                {
                    case "ellipse":
                        myPath.AddEllipse(selectObj.Rectangle);
                        break;

                    case "rectangle":
                        myPath.AddRectangle(selectObj.Rectangle);
                        break;
                }

                CreateBorderPoints();

                ShowToolPanel();

                _pictureBox.Invalidate();

            }
            #endregion

            #region Создание объекта
            if (borderCreating)
            {
                if (e.X > 25 && e.X < _pictureBox.Image.Width - 25
                    && e.Y > 25 && e.Y < _pictureBox.Image.Height - 25)
                {
                    if (!IntersectNewObject(new Point(e.X, e.Y)))
                    {
                        borderCreating = false;
                        drawingBrdObjs = true;
                        normalMode = true;

                        _pictureBox.Cursor = Cursors.Default;

                        selectObj = new ImgData(new Point(e.X - 25, e.Y - 25));

                        myPath = new GraphicsPath();

                        myPath.AddEllipse(selectObj.Rectangle);

                        CreateBorderPoints();

                        _dataGridView.Rows.Add(selectObj.ID, "");

                        _dataGridView.CurrentCell = _dataGridView.Rows[_dataGridView.RowCount - 1].Cells[1];

                        infList.Add(selectObj);

                        ShowToolPanel();

                        _pictureBox.Invalidate();
                    }
                }
            }
            #endregion

            #region Конец перемещения объекта и изменения размеров объекта
            if (!normalMode && mouseInSlctObj || !normalMode && borderChMsIn.ContainsValue(true))
            {
                normalMode = true;
                drawingBrdObjs = true;

                if (tempObjDrawing)
                    tempObjDrawing = false;

                Cursor.Clip = screen;

                selectObj.Rectangle = draggedRect;

                myPath = new GraphicsPath();

                switch (selectObj.Shape)
                {
                    case "ellipse":
                        myPath.AddEllipse(selectObj.Rectangle);
                        break;

                    case "rectangle":
                        myPath.AddRectangle(selectObj.Rectangle);
                        break;
                }

                CreateBorderPoints();

                ShowToolPanel();

                _pictureBox.Invalidate();
            }
            #endregion
        }

        private void pictureBox_Paint(object sender, PaintEventArgs e)
        {
            #region Перерисовка всех объектов
            if (redrawingObjs)
            {
                redrawingObjs = false;
                RedrawingObjects();
            }
            #endregion

            #region Перерисовка всех объектов + отрисовка рамки
            if (drawingBrdObjs)
            {
                drawingBrdObjs = false;

                RedrawingObjects();

                if (selectObj.BorderWidth != 0)
                    PaintBorder();
            }
            #endregion

            #region Отрисовка подсветки
            if (drawingLighting && findObj != null && findObj != selectObj)
            {
                drawingLighting = false;

                PaintLighting(findObj);
            }
            #endregion

            #region Отрисовка объекта при перемещении и изменении размеров
            if (tempObjDrawing)
            {
                tempObjDrawing = false;

                RedrawingUnslctObjects();

                PaintMovingObj(draggedRect);
            }
            #endregion
        }

        private void pictureBox_MouseEnter(object sender, EventArgs e)
        {
            #region Вхождение в курсора мыши в область pictureBox при создании объекта
            if (borderCreating)
                _pictureBox.Cursor = Cursors.Cross;
            #endregion
        }

        private void pictureBox_MouseLeave(object sender, EventArgs e)
        {
            #region Выход курсора мыши из области pictureBox в нормальном режиме
            if (normalMode)
            {
                if (mouseInSlctObj)
                    mouseInSlctObj = false;

                if (borderChMsIn.ContainsValue(true))
                {
                    borderChMsIn["outlineBoxLftBot"] = false;
                    borderChMsIn["outlineBoxLftMid"] = false;
                    borderChMsIn["outlineBoxLftTop"] = false;
                    borderChMsIn["outlineBoxTopMid"] = false;
                    borderChMsIn["outlineBoxRghtTop"] = false;
                    borderChMsIn["outlineBoxRghtMid"] = false;
                    borderChMsIn["outlineBoxRghtBot"] = false;
                    borderChMsIn["outlineBoxBotMid"] = false;
                }

                _pictureBox.Cursor = Cursors.Default;

                if (selectObj != null)
                    drawingBrdObjs = true;
                else
                    redrawingObjs = true;

                _pictureBox.Invalidate();
            }
            #endregion
        }

        private void RedrawingObjects()
        {
            _pictureBox.Image = image;

            if (bitmapImage != null)
                bitmapImage.Dispose();

            bitmapImage = new Bitmap(_pictureBox.Image);
            using (Graphics g = Graphics.FromImage(bitmapImage))
            {
                foreach (var obj in infList)
                {
                    PaintObject(g, obj);
                }
            }
            _pictureBox.Image = bitmapImage;
        }

        private void RedrawingUnslctObjects()
        {
            _pictureBox.Image = image;

            if (bitmapImage != null)
                bitmapImage.Dispose();

            bitmapImage = new Bitmap(_pictureBox.Image);
            using (Graphics g = Graphics.FromImage(bitmapImage))
            {
                foreach (var obj in infList)
                {
                    if (obj != selectObj)
                        PaintObject(g, obj);
                }
            }
        }

        private void PaintObject(Graphics g, ImgData obj)
        {
            //Объект
            Brush brushObj = new SolidBrush(Color.FromArgb(obj.Transp, obj.ColorObj));

            //Отрисовка выделения
            switch (obj.Shape)
            {
                case "ellipse":
                    g.FillEllipse(brushObj, obj.Rectangle.X, obj.Rectangle.Y,
                        obj.Rectangle.Width, obj.Rectangle.Height);
                    break;
                case "rectangle":
                    g.FillRectangle(brushObj, obj.Rectangle.X, obj.Rectangle.Y,
                        obj.Rectangle.Width, obj.Rectangle.Height);
                    break;
            }
        }

        private void PaintMovingObj(Rectangle obj)
        {
            //Объект
            Brush brushObj = new SolidBrush(Color.FromArgb(selectObj.Transp, selectObj.ColorObj));

            using (Graphics g = Graphics.FromImage(bitmapImage))
            {
                //Отрисовка выделения
                switch (selectObj.Shape)
                {
                    case "ellipse":
                        g.FillEllipse(brushObj, obj.X, obj.Y, obj.Width, obj.Height);
                        break;
                    case "rectangle":
                        g.FillRectangle(brushObj, obj.X, obj.Y, obj.Width, obj.Height);
                        break;
                }
            }
            _pictureBox.Image = bitmapImage;
        }

        private void PaintBorder()
        {
            //Рамка
            Pen penBrd = new Pen(Color.FromArgb(selectObj.Transp, selectObj.ColorBrd), selectObj.BorderWidth);
            if (selectObj.Dash)
                penBrd.DashStyle = DashStyle.Dash;

            Pen penOutBox = new Pen(Color.Black, 1);

            Brush brushOutBox = new SolidBrush(Color.White);

            int outBoxWidth = 4;
            if (selectObj.BorderWidth > 2)
            {
                if (selectObj.BorderWidth % 2 == 0)
                    outBoxWidth = selectObj.BorderWidth;
                else
                    outBoxWidth = ++selectObj.BorderWidth;
            }

            using (Graphics g = Graphics.FromImage(bitmapImage))
            {
                g.DrawRectangle(penBrd, selectObj.Rectangle.X, selectObj.Rectangle.Y,
                       selectObj.Rectangle.Width, selectObj.Rectangle.Height);

                //Отрисовка точек на рамке

                //Отрисовка левой нижней области
                g.FillRectangle(brushOutBox, outlineBoxLftBot.X + 5 - outBoxWidth / 2,
                    outlineBoxLftBot.Y + 5 - outBoxWidth / 2, outBoxWidth, outBoxWidth);
                g.DrawRectangle(penOutBox, outlineBoxLftBot.X + 5 - outBoxWidth / 2,
                    outlineBoxLftBot.Y + 5 - outBoxWidth / 2, outBoxWidth, outBoxWidth);

                //Отрисовка левой средней области
                g.FillRectangle(brushOutBox, outlineBoxLftMid.X + 5 - outBoxWidth / 2,
                    outlineBoxLftMid.Y + 5 - outBoxWidth / 2, outBoxWidth, outBoxWidth);
                g.DrawRectangle(penOutBox, outlineBoxLftMid.X + 5 - outBoxWidth / 2,
                    outlineBoxLftMid.Y + 5 - outBoxWidth / 2, outBoxWidth, outBoxWidth);

                //Отрисовка левой верхней области
                g.FillRectangle(brushOutBox, outlineBoxLftTop.X + 5 - outBoxWidth / 2,
                    outlineBoxLftTop.Y + 5 - outBoxWidth / 2, outBoxWidth, outBoxWidth);
                g.DrawRectangle(penOutBox, outlineBoxLftTop.X + 5 - outBoxWidth / 2,
                    outlineBoxLftTop.Y + 5 - outBoxWidth / 2, outBoxWidth, outBoxWidth);

                //Отрисовка верхней средней области
                g.FillRectangle(brushOutBox, outlineBoxTopMid.X + 5 - outBoxWidth / 2,
                    outlineBoxTopMid.Y + 5 - outBoxWidth / 2, outBoxWidth, outBoxWidth);
                g.DrawRectangle(penOutBox, outlineBoxTopMid.X + 5 - outBoxWidth / 2,
                    outlineBoxTopMid.Y + 5 - outBoxWidth / 2, outBoxWidth, outBoxWidth);

                //Отрисовка правой верхней области
                g.FillRectangle(brushOutBox, outlineBoxRghtTop.X + 5 - outBoxWidth / 2,
                    outlineBoxRghtTop.Y + 5 - outBoxWidth / 2, outBoxWidth, outBoxWidth);
                g.DrawRectangle(penOutBox, outlineBoxRghtTop.X + 5 - outBoxWidth / 2,
                    outlineBoxRghtTop.Y + 5 - outBoxWidth / 2, outBoxWidth, outBoxWidth);

                //Отрисовка правой средней области
                g.FillRectangle(brushOutBox, outlineBoxRghtMid.X + 5 - outBoxWidth / 2,
                    outlineBoxRghtMid.Y + 5 - outBoxWidth / 2, outBoxWidth, outBoxWidth);
                g.DrawRectangle(penOutBox, outlineBoxRghtMid.X + 5 - outBoxWidth / 2,
                    outlineBoxRghtMid.Y + 5 - outBoxWidth / 2, outBoxWidth, outBoxWidth);

                //Отрисовка правой нижней области
                g.FillRectangle(brushOutBox, outlineBoxRghtBot.X + 5 - outBoxWidth / 2,
                    outlineBoxRghtBot.Y + 5 - outBoxWidth / 2, outBoxWidth, outBoxWidth);
                g.DrawRectangle(penOutBox, outlineBoxRghtBot.X + 5 - outBoxWidth / 2,
                    outlineBoxRghtBot.Y + 5 - outBoxWidth / 2, outBoxWidth, outBoxWidth);

                //Отрисовка средней нижней области
                g.FillRectangle(brushOutBox, outlineBoxBotMid.X + 5 - outBoxWidth / 2,
                    outlineBoxBotMid.Y + 5 - outBoxWidth / 2, outBoxWidth, outBoxWidth);
                g.DrawRectangle(penOutBox, outlineBoxBotMid.X + 5 - outBoxWidth / 2,
                    outlineBoxBotMid.Y + 5 - outBoxWidth / 2, outBoxWidth, outBoxWidth);
            }
            _pictureBox.Image = bitmapImage;
        }

        private void CreateBorderPoints()
        {
            //Создание областей изменения размеров на выделении
            outlineBoxLftBot = new Rectangle(selectObj.Rectangle.X - 5, selectObj.Rectangle.Y +
                selectObj.Rectangle.Height - 5, 10, 10);

            outlineBoxLftMid = new Rectangle(selectObj.Rectangle.X - 5,
                selectObj.Rectangle.Y + selectObj.Rectangle.Height / 2 - 5, 10, 10);

            outlineBoxLftTop = new Rectangle(selectObj.Rectangle.X - 5, selectObj.Rectangle.Y - 5, 10, 10);

            outlineBoxTopMid = new Rectangle(selectObj.Rectangle.X + selectObj.Rectangle.Width / 2 - 5,
                selectObj.Rectangle.Y - 5, 10, 10);

            outlineBoxRghtTop = new Rectangle(selectObj.Rectangle.X + selectObj.Rectangle.Width - 5,
                selectObj.Rectangle.Y - 5, 10, 10);

            outlineBoxRghtMid = new Rectangle(selectObj.Rectangle.X + selectObj.Rectangle.Width - 5,
                selectObj.Rectangle.Y + selectObj.Rectangle.Height / 2 - 5, 10, 10);

            outlineBoxRghtBot = new Rectangle(selectObj.Rectangle.X + selectObj.Rectangle.Width - 5,
                 selectObj.Rectangle.Y + selectObj.Rectangle.Height - 5, 10, 10);

            outlineBoxBotMid = new Rectangle(selectObj.Rectangle.X + selectObj.Rectangle.Width / 2 - 5,
                selectObj.Rectangle.Y + selectObj.Rectangle.Height - 5, 10, 10);
        }

        private void PaintLighting(ImgData obj)
        {
            //Объект
            Brush brushObj = new SolidBrush(Color.FromArgb(60, Color.Yellow));

            using (Graphics g = Graphics.FromImage(bitmapImage))
            {
                switch (obj.Shape)
                {
                    case "ellipse":
                        g.FillEllipse(brushObj, obj.Rectangle.X, obj.Rectangle.Y,
                                obj.Rectangle.Width, obj.Rectangle.Height);
                        break;
                    case "rectangle":
                        g.FillRectangle(brushObj, obj.Rectangle.X, obj.Rectangle.Y,
                        obj.Rectangle.Width, obj.Rectangle.Height);
                        break;
                }
            }
            _pictureBox.Image = bitmapImage;
        }

        //Проверка попадания курсора мыши в объект с возвращением данного объекта
        private ImgData ContainObject(Point lastMousePos)
        {
            ImgData _findObj = null;

            foreach (var obj in infList)
            {
                GraphicsPath path = new GraphicsPath();

                switch (obj.Shape)
                {
                    case "ellipse":
                        path.AddEllipse(obj.Rectangle);
                        break;

                    case "rectangle":
                        path.AddRectangle(obj.Rectangle);
                        break;
                }

                if (path.IsVisible(lastMousePos))
                {
                    _findObj = obj;
                    break;
                }
            }
            return _findObj;
        }

        private bool IntersectionObjectMv(Point position)
        {
            Rectangle newRect = new Rectangle(position.X - deltaX, position.Y - deltaY, draggedRect.Width, draggedRect.Height);

            GraphicsPath newPath = new GraphicsPath();

            switch (selectObj.Shape)
            {
                case "ellipse":
                    newPath.AddEllipse(newRect);
                    break;

                case "rectangle":
                    newPath.AddRectangle(newRect);
                    break;
            }

            foreach (var obj in infList)
            {
                if (selectObj != obj)
                {
                    GraphicsPath path = new GraphicsPath();

                    switch (obj.Shape)
                    {
                        case "ellipse":
                            path.AddEllipse(obj.Rectangle);
                            break;

                        case "rectangle":
                            path.AddRectangle(obj.Rectangle);
                            break;
                    }

                    foreach (var point in path.PathPoints)
                        if (newPath.IsVisible(point))
                            return true;
                }

            }

            foreach (var point in newPath.PathData.Points)
                if (ContainPoint(point))
                    return true;

            return false;
        }

        private bool IntersectionObjectChg(Rectangle newRect)
        {
            GraphicsPath newPath = new GraphicsPath();

            switch (selectObj.Shape)
            {
                case "ellipse":
                    newPath.AddEllipse(newRect);
                    break;

                case "rectangle":
                    newPath.AddRectangle(newRect);
                    break;
            }

            foreach (var obj in infList)
            {
                if (selectObj != obj)
                {
                    GraphicsPath path = new GraphicsPath();

                    switch (obj.Shape)
                    {
                        case "ellipse":
                            path.AddEllipse(obj.Rectangle);
                            break;

                        case "rectangle":
                            path.AddRectangle(obj.Rectangle);
                            break;
                    }

                    foreach (var point in path.PathPoints)
                        if (newPath.IsVisible(point))
                            return true;
                }

            }

            foreach (var point in newPath.PathData.Points)
                if (ContainPoint(point))
                    return true;

            return false;
        }


        //Проверка попадания точки в другие объекты
        private bool IntersectNewObject(Point position)
        {
            Rectangle newRect = new Rectangle(position.X - 25, position.Y - 25, 50, 50);

            GraphicsPath newPath = new GraphicsPath();

            newPath.AddEllipse(newRect);

            foreach (var obj in infList)
            {
                GraphicsPath path = new GraphicsPath();

                switch (obj.Shape)
                {
                    case "ellipse":
                        path.AddEllipse(obj.Rectangle);
                        break;

                    case "rectangle":
                        path.AddRectangle(obj.Rectangle);
                        break;
                }

                foreach (var point in path.PathPoints)
                    if (newPath.IsVisible(point))
                        return true;
            }

            foreach (var point in newPath.PathData.Points)
                if (ContainNewPoint(point))
                    return true;

            return false;
        }

        //Проверка попадания точки в другие объекты
        private bool ContainNewPoint(PointF point)
        {
            foreach (var obj in infList)
            {
                GraphicsPath path = new GraphicsPath();

                switch (obj.Shape)
                {
                    case "ellipse":
                        path.AddEllipse(obj.Rectangle);
                        break;

                    case "rectangle":
                        path.AddRectangle(obj.Rectangle);
                        break;
                }

                if (path.IsVisible(point))
                    return true;
            }
            return false;
        }

        //Проверка попадания точки в другие объекты
        private bool ContainPoint(PointF point)
        {
            foreach (var obj in infList)
                if (obj != selectObj)
                {
                    GraphicsPath path = new GraphicsPath();

                    switch (obj.Shape)
                    {
                        case "ellipse":
                            path.AddEllipse(obj.Rectangle);
                            break;

                        case "rectangle":
                            path.AddRectangle(obj.Rectangle);
                            break;
                    }

                    if (path.IsVisible(point))
                        return true;
                }
            return false;
        }
        #endregion
        #endregion

        #region dataGridView
        DataGridView _dataGridView;

        void SetDataGridView(DataGridView dataGridView)
        {
            _dataGridView = dataGridView;

            _dataGridView.CellEndEdit += dataGridView_CellEndEdit;
            _dataGridView.CellClick += dataGridView_CellClick;

            var column0 = new DataGridViewColumn();
            column0.HeaderText = "ID"; //текст в шапке
            column0.Width = 50; //ширина колонки
            column0.ReadOnly = true; //значение в этой колонке нельзя править
            column0.Name = "id"; //текстовое имя колонки, его можно использовать вместо обращений по индексу
            column0.Frozen = true; //флаг, что данная колонка всегда отображается на своем месте
            column0.CellTemplate = new DataGridViewTextBoxCell(); //тип нашей колонки

            var column1 = new DataGridViewColumn();
            column1.HeaderText = "Name"; //текст в шапке
            column1.Width = 170; //ширина колонки
            column1.Name = "name"; //текстовое имя колонки, его можно использовать вместо обращений по индексу
            column1.Frozen = true; //флаг, что данная колонка всегда отображается на своем месте
            column1.CellTemplate = new DataGridViewTextBoxCell(); //тип нашей колонки

            dataGridView.Columns.Add(column0);
            dataGridView.Columns.Add(column1);

            dataGridView.AllowUserToAddRows = false; //запрешаем пользователю самому добавлять строки
            _dataGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;//выделяем всю строку по нажатию
            _dataGridView.MultiSelect = false;
            _dataGridView.AllowUserToResizeColumns = false;
            _dataGridView.AllowUserToResizeRows = false;
            _dataGridView.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;

            foreach (var obj in infList)
            {
                dataGridView.Rows.Add(obj.ID, obj.Name);
            }

            //Отмена выделения
            _dataGridView.ClearSelection();
        }

        //TO DO сделать выделение по щелчку на поле
        private void dataGridView_CellClick(object s, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex > -1 && normalMode)
            {
                    foreach (var obj in infList)
                        if (_dataGridView.Rows[e.RowIndex].Cells[0].Value != null)
                            if (_dataGridView.Rows[e.RowIndex].Cells[0].Value.Equals(obj.ID))
                            {
                                drawingBrdObjs = true;

                                selectObj = obj;

                                myPath = new GraphicsPath();

                                switch (selectObj.Shape)
                                {
                                    case "elipse":
                                        myPath.AddEllipse(selectObj.Rectangle);
                                        break;

                                    case "rectangle":
                                        myPath.AddRectangle(selectObj.Rectangle);
                                        break;
                                }
                                CreateBorderPoints();

                                ShowToolPanel();

                                _pictureBox.Invalidate();

                                break;
                        }
            }
            else
                if (!normalMode)
                _dataGridView.ClearSelection();
        }

        void dataGridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            foreach (var obj in infList)
                if (obj.ID == Convert.ToInt32(_dataGridView.Rows[e.RowIndex].Cells[0].Value))
                {
                    obj.Name = _dataGridView.Rows[e.RowIndex].Cells[1].Value.ToString();
                    break;
                }
        }
        #endregion

        #region Controls панель инструментов и связанные с ними методы
        //Button create object
        void CreateButton()
        {
            btnCrt = new Button();
            btnCrt.Width = 50;
            btnCrt.Location = new Point(5, 30);
            btnCrt.Text = "Create";

            btnCrt.Click += delegate
            {
                if (normalMode)
                    normalMode = false;

                if (toolPnl.Visible)
                    HideToolPanel();

                if (!borderCreating)
                    borderCreating = true;

                if (!redrawingObjs)
                    redrawingObjs = true;

                //Отмена выделения
                _dataGridView.ClearSelection();
            };

            _form.Controls.Add(btnCrt);
        }

        //Panel
        void CreatePanel()
        {
            ToolTip t = new ToolTip();

            toolPnl = new Panel();
            toolPnl.BorderStyle = BorderStyle.FixedSingle;
            toolPnl.Visible = false;
            toolPnl.Height = 242;
            toolPnl.Width = 32;

            //Цвет рамки
            Button btnClrB = new Button();
            btnClrB.BackgroundImage = Image.FromFile(@"buttons\borderColor.png", false);
            btnClrB.BackgroundImageLayout = ImageLayout.Stretch;
            btnClrB.Location = new Point(0, 0);
            btnClrB.Height = 30;
            btnClrB.Width = 30;
            t.SetToolTip(btnClrB, "Border color");
            btnClrB.Tag = toolPnl;

            #region Панель цвета рамки
            pnlClrB = new Panel();
            pnlClrB.BorderStyle = BorderStyle.FixedSingle;
            pnlClrB.Height = 30;
            pnlClrB.Width = 200;
            pnlClrB.Visible = false;

            //Изображение для цвета обводки
            PictureBox pbClrB = new PictureBox();
            pbClrB.Location = new Point(178, 5);
            pbClrB.Width = 16;
            pbClrB.Height = 18;

            //Надпись к цвету рамки R
            Label lblClrBR = new Label();
            lblClrBR.Text = "R:";
            lblClrBR.Width = 18;
            lblClrBR.Location = new Point(0, 6);

            //Поле для значения цвета рамки R
            NumericUpDown nmrcClrBR = new NumericUpDown();
            nmrcClrBR.Location = new Point(18, 4);
            nmrcClrBR.Width = 40;
            nmrcClrBR.Maximum = 255;
            nmrcClrBR.Minimum = 0;
            nmrcClrBR.KeyUp += delegate
            {
                drawingBrdObjs = true;

                selectObj.ColorBrd = Color.FromArgb(Convert.ToByte(nmrcClrBR.Value), selectObj.ColorBrd.G, selectObj.ColorBrd.B);
                pbClrB.BackColor = selectObj.ColorBrd;

                _pictureBox.Invalidate();
            };
            nmrcClrBR.ValueChanged += delegate
            {
                drawingBrdObjs = true;

                selectObj.ColorBrd = Color.FromArgb(Convert.ToByte(nmrcClrBR.Value), selectObj.ColorBrd.G, selectObj.ColorBrd.B);
                pbClrB.BackColor = selectObj.ColorBrd;

                _pictureBox.Invalidate();
            };

            //Надпись к цвету рамки G
            Label lblClrBG = new Label();
            lblClrBG.Text = "G:";
            lblClrBG.Width = 18;
            lblClrBG.Location = new Point(58, 6);

            //Поле для значения цвета рамки G
            NumericUpDown nmrcClrBG = new NumericUpDown();
            nmrcClrBG.Location = new Point(76, 4);
            nmrcClrBG.Width = 40;
            nmrcClrBG.Maximum = 255;
            nmrcClrBG.Minimum = 0;
            nmrcClrBG.KeyUp += delegate
            {
                drawingBrdObjs = true;

                selectObj.ColorBrd = Color.FromArgb(selectObj.ColorBrd.R, Convert.ToByte(nmrcClrBG.Value), selectObj.ColorBrd.B);
                pbClrB.BackColor = selectObj.ColorBrd;

                _pictureBox.Invalidate();
            };
            nmrcClrBG.ValueChanged += delegate
            {
                drawingBrdObjs = true;

                selectObj.ColorBrd = Color.FromArgb(selectObj.ColorBrd.R, Convert.ToByte(nmrcClrBG.Value), selectObj.ColorBrd.B);
                pbClrB.BackColor = selectObj.ColorBrd;

                _pictureBox.Invalidate();
            };

            //Надпись к цвету рамки B
            Label lblClrBB = new Label();
            lblClrBB.Text = "B:";
            lblClrBB.Width = 18;
            lblClrBB.Location = new Point(116, 6);

            //Поле для значения цвета рамки B
            NumericUpDown nmrcClrBB = new NumericUpDown();
            nmrcClrBB.Location = new Point(134, 4);
            nmrcClrBB.Width = 40;
            nmrcClrBB.Maximum = 255;
            nmrcClrBB.Minimum = 0;
            nmrcClrBB.KeyUp += delegate
            {
                drawingBrdObjs = true;

                selectObj.ColorBrd = Color.FromArgb(selectObj.ColorBrd.R, selectObj.ColorBrd.G, Convert.ToByte(nmrcClrBB.Value));
                pbClrB.BackColor = selectObj.ColorBrd;

                _pictureBox.Invalidate();
            };
            nmrcClrBB.ValueChanged += delegate
            {
                drawingBrdObjs = true;

                selectObj.ColorBrd = Color.FromArgb(selectObj.ColorBrd.R, selectObj.ColorBrd.G, Convert.ToByte(nmrcClrBB.Value));
                pbClrB.BackColor = selectObj.ColorBrd;

                _pictureBox.Invalidate();
            };
            #endregion

            btnClrB.Click += delegate
            {
                nmrcClrBR.Text = selectObj.ColorBrd.R.ToString();
                nmrcClrBG.Text = selectObj.ColorBrd.G.ToString();
                nmrcClrBB.Text = selectObj.ColorBrd.B.ToString();

                pbClrB.BackColor = selectObj.ColorBrd;

                if (!pnlClrB.Visible)
                {
                    Point location = new Point(toolPnl.Location.X + toolPnl.Width, toolPnl.Location.Y);

                    if (location.X + pnlClrB.Width < _pictureBox.Width)
                        pnlClrB.Location = location;
                    else
                        pnlClrB.Location = new Point(toolPnl.Location.X - pnlClrB.Width, toolPnl.Location.Y);

                    pnlClrB.Visible = true;
                }
                else
                    pnlClrB.Visible = false;
            };

            //Толщина рамки
            Button btnWdthB = new Button();
            btnWdthB.BackgroundImage = Image.FromFile(@"buttons\borderWidth.png", false);
            btnWdthB.BackgroundImageLayout = ImageLayout.Stretch;
            btnWdthB.Location = new Point(0, 35);
            btnWdthB.Height = 30;
            btnWdthB.Width = 30;
            t.SetToolTip(btnWdthB, "Border width");
            btnWdthB.Tag = toolPnl;

            #region Панель изменения ширины рамки
            pnlWdthB = new Panel();
            pnlWdthB.BorderStyle = BorderStyle.FixedSingle;
            pnlWdthB.Height = 30;
            pnlWdthB.Width = 110;
            pnlWdthB.Visible = false;

            //Надпись к текстовому полю ширины обводки
            Label lblBWidth = new Label();
            lblBWidth.Text = "Border width:";
            lblBWidth.Location = new Point(0, 6);
            lblBWidth.Width = 70;

            //Ширина обводки
            NumericUpDown nmrcBWidth = new NumericUpDown();
            nmrcBWidth.Location = new Point(70, 4);
            nmrcBWidth.Width = 35;
            nmrcBWidth.Maximum = 10;
            nmrcBWidth.Minimum = 0;
            nmrcBWidth.KeyUp += delegate
            {
                drawingBrdObjs = true;

                selectObj.BorderWidth = Convert.ToInt32(nmrcBWidth.Value);

                _pictureBox.Invalidate();
            };
            nmrcBWidth.ValueChanged += delegate
            {
                drawingBrdObjs = true;

                selectObj.BorderWidth = Convert.ToInt32(nmrcBWidth.Value);

                _pictureBox.Invalidate();
            };
            #endregion

            btnWdthB.Click += delegate
            {
                nmrcBWidth.Text = selectObj.BorderWidth.ToString();

                if (!pnlWdthB.Visible)
                {
                    Point location = new Point(toolPnl.Location.X + toolPnl.Width, toolPnl.Location.Y + 35);

                    if (location.X + pnlWdthB.Width < _pictureBox.Width)
                        pnlWdthB.Location = location;
                    else
                        pnlWdthB.Location = new Point(toolPnl.Location.X - pnlWdthB.Width, toolPnl.Location.Y + 35);

                    pnlWdthB.Visible = true;
                }
                else
                    pnlWdthB.Visible = false;
            };

            //Пунктирность рамки
            Button btnDshB = new Button();
            btnDshB.BackgroundImage = Image.FromFile(@"buttons\borderDash.png", false);
            btnDshB.BackgroundImageLayout = ImageLayout.Stretch;
            btnDshB.Location = new Point(0, 70);
            btnDshB.Height = 30;
            btnDshB.Width = 30;
            t.SetToolTip(btnDshB, "Border dash");
            btnDshB.Tag = toolPnl;

            #region Панель изменения пунктира рамки
            pnlDshB = new Panel();
            pnlDshB.BorderStyle = BorderStyle.FixedSingle;
            pnlDshB.Height = 30;
            pnlDshB.Width = 130;
            pnlDshB.Visible = false;

            //Надпись к текстовому полю пунктира
            Label lblDsh = new Label();
            lblDsh.Text = "Dash:";
            lblDsh.Width = 40;
            lblDsh.Location = new Point(0, 6);

            //Радио кнопка пунктира да
            RadioButton btnDshYes = new RadioButton();
            btnDshYes.Text = "Yes";
            btnDshYes.Width = 50;
            btnDshYes.Location = new Point(40, 2);
            btnDshYes.CheckedChanged += delegate
            {
                drawingBrdObjs = true;

                if (btnDshYes.Checked)
                    selectObj.Dash = true;

                _pictureBox.Invalidate();
            };

            //Радио кнопка пунктира нет
            RadioButton btnDshNo = new RadioButton();
            btnDshNo.Text = "No";
            btnDshNo.Width = 40;
            btnDshNo.Location = new Point(90, 2);
            btnDshNo.CheckedChanged += delegate
            {
                drawingBrdObjs = true;

                if (btnDshNo.Checked)
                    selectObj.Dash = false;

                _pictureBox.Invalidate();
            };

            #endregion

            btnDshB.Click += delegate
            {
                if (selectObj.Dash)
                    btnDshYes.Checked = true;
                else
                    btnDshNo.Checked = true;

                if (!pnlDshB.Visible)
                {
                    Point location = new Point(toolPnl.Location.X + toolPnl.Width, toolPnl.Location.Y + 70);

                    if (location.X + pnlDshB.Width < _pictureBox.Width)
                        pnlDshB.Location = location;
                    else
                        pnlDshB.Location = new Point(toolPnl.Location.X - pnlDshB.Width, toolPnl.Location.Y + 70);

                    pnlDshB.Visible = true;
                }
                else
                    pnlDshB.Visible = false;

            };

            //Цвет заливки
            Button btnClrOb = new Button();
            btnClrOb.BackgroundImage = Image.FromFile(@"buttons\fillColor.png", false);
            btnClrOb.BackgroundImageLayout = ImageLayout.Stretch;
            btnClrOb.Location = new Point(0, 105);
            btnClrOb.Height = 30;
            btnClrOb.Width = 30;
            t.SetToolTip(btnClrOb, "Fill color");
            btnClrOb.Tag = toolPnl;

            #region Панель цвета заливки
            pnlClrO = new Panel();
            pnlClrO.BorderStyle = BorderStyle.FixedSingle;
            pnlClrO.Height = 30;
            pnlClrO.Width = 200;
            pnlClrO.Visible = false;

            //Изображение для цвета обводки
            PictureBox pbClrO = new PictureBox();
            pbClrO.Location = new Point(178, 5);
            pbClrO.Width = 16;
            pbClrO.Height = 18;

            //Надпись к цвету рамки R
            Label lblClrOR = new Label();
            lblClrOR.Text = "R:";
            lblClrOR.Width = 18;
            lblClrOR.Location = new Point(0, 6);

            //Поле для значения цвета рамки R
            NumericUpDown nmrcClrOR = new NumericUpDown();
            nmrcClrOR.Location = new Point(18, 4);
            nmrcClrOR.Width = 40;
            nmrcClrOR.Maximum = 255;
            nmrcClrOR.Minimum = 0;
            nmrcClrOR.KeyUp += delegate
            {
                drawingBrdObjs = true;

                selectObj.ColorObj = Color.FromArgb(Convert.ToByte(nmrcClrOR.Value), selectObj.ColorObj.G, selectObj.ColorObj.B);
                pbClrO.BackColor = selectObj.ColorObj;

                _pictureBox.Invalidate();
            };
            nmrcClrOR.ValueChanged += delegate
            {
                drawingBrdObjs = true;

                selectObj.ColorObj = Color.FromArgb(Convert.ToByte(nmrcClrOR.Value), selectObj.ColorObj.G, selectObj.ColorObj.B);
                pbClrO.BackColor = selectObj.ColorObj;

                _pictureBox.Invalidate();
            };

            //Надпись к цвету рамки G
            Label lblClrOG = new Label();
            lblClrOG.Text = "G:";
            lblClrOG.Width = 18;
            lblClrOG.Location = new Point(58, 6);

            //Поле для значения цвета рамки G
            NumericUpDown nmrcClrOG = new NumericUpDown();
            nmrcClrOG.Location = new Point(76, 4);
            nmrcClrOG.Width = 40;
            nmrcClrOG.Maximum = 255;
            nmrcClrOG.Minimum = 0;
            nmrcClrOG.KeyUp += delegate
            {
                drawingBrdObjs = true;

                selectObj.ColorObj = Color.FromArgb(selectObj.ColorObj.R, Convert.ToByte(nmrcClrOG.Value), selectObj.ColorObj.B);
                pbClrO.BackColor = selectObj.ColorObj;

                _pictureBox.Invalidate();
            };
            nmrcClrOG.ValueChanged += delegate
            {
                drawingBrdObjs = true;

                selectObj.ColorObj = Color.FromArgb(selectObj.ColorObj.R, Convert.ToByte(nmrcClrOG.Value), selectObj.ColorObj.B);
                pbClrO.BackColor = selectObj.ColorObj;

                _pictureBox.Invalidate();
            };

            //Надпись к цвету рамки B
            Label lblClrOB = new Label();
            lblClrOB.Text = "B:";
            lblClrOB.Width = 18;
            lblClrOB.Location = new Point(116, 6);

            //Поле для значения цвета рамки B
            NumericUpDown nmrcClrOB = new NumericUpDown();
            nmrcClrOB.Location = new Point(134, 4);
            nmrcClrOB.Width = 40;
            nmrcClrOB.Maximum = 255;
            nmrcClrOB.Minimum = 0;
            nmrcClrOB.KeyUp += delegate
            {
                drawingBrdObjs = true;

                selectObj.ColorObj = Color.FromArgb(selectObj.ColorObj.R, selectObj.ColorObj.G, Convert.ToByte(nmrcClrOB.Value));
                pbClrO.BackColor = selectObj.ColorObj;

                _pictureBox.Invalidate();
            };
            nmrcClrOB.ValueChanged += delegate
            {
                drawingBrdObjs = true;

                selectObj.ColorObj = Color.FromArgb(selectObj.ColorObj.R, selectObj.ColorObj.G, Convert.ToByte(nmrcClrOB.Value));
                pbClrO.BackColor = selectObj.ColorObj;

                _pictureBox.Invalidate();
            };
            #endregion

            btnClrOb.Click += delegate
            {
                nmrcClrOR.Text = selectObj.ColorObj.R.ToString();
                nmrcClrOG.Text = selectObj.ColorObj.G.ToString();
                nmrcClrOB.Text = selectObj.ColorObj.B.ToString();

                pbClrO.BackColor = selectObj.ColorObj;

                if (!pnlClrO.Visible)
                {
                    Point location = new Point(toolPnl.Location.X + toolPnl.Width, toolPnl.Location.Y + 105);

                    if (location.X + pnlClrO.Width < _pictureBox.Width)
                        pnlClrO.Location = location;
                    else
                        pnlClrO.Location = new Point(toolPnl.Location.X - pnlClrO.Width, toolPnl.Location.Y + 105);

                    pnlClrO.Visible = true;
                }
                else
                    pnlClrO.Visible = false;
            };

            //Форма
            Button btnShp = new Button();
            btnShp.BackgroundImage = Image.FromFile(@"buttons\shape.png", false);
            btnShp.BackgroundImageLayout = ImageLayout.Stretch;
            btnShp.Location = new Point(0, 140);
            btnShp.Height = 30;
            btnShp.Width = 30;
            t.SetToolTip(btnShp, "Shape");
            btnShp.Tag = toolPnl;

            #region Панель изменения формы объекта
            pnlShpB = new Panel();
            pnlShpB.BorderStyle = BorderStyle.FixedSingle;
            pnlShpB.Height = 30;
            pnlShpB.Width = 172;
            pnlShpB.Visible = false;

            Label lblShp = new Label();
            lblShp.Text = "Shape:";
            lblShp.Width = 42;
            lblShp.Location = new Point(0, 6);

            //Радио кнопка эллипса
            RadioButton btnEl = new RadioButton();
            btnEl.Text = "Ellipse";
            btnEl.Width = 55;
            btnEl.Location = new Point(42, 2);
            btnEl.CheckedChanged += delegate
            {
                drawingBrdObjs = true;

                if (btnEl.Checked)
                    selectObj.Shape = "ellipse";

                _pictureBox.Invalidate();
            };

            //Радио кнопка прямоугольника
            RadioButton btnRc = new RadioButton();
            btnRc.Text = "Rectangle";
            btnRc.Width = 75;
            btnRc.Location = new Point(97, 2);
            btnRc.CheckedChanged += delegate
            {
                drawingBrdObjs = true;

                if (btnRc.Checked)
                    selectObj.Shape = "rectangle";

                _pictureBox.Invalidate();
            };
            #endregion

            btnShp.Click += delegate
            {
                if (selectObj.Shape == "ellipse")
                    btnEl.Checked = true;
                else
                    btnRc.Checked = true;

                if (!pnlShpB.Visible)
                {
                    Point location = new Point(toolPnl.Location.X + toolPnl.Width, toolPnl.Location.Y + 140);

                    if (location.X + pnlShpB.Width < _pictureBox.Width)
                        pnlShpB.Location = location;
                    else
                        pnlShpB.Location = new Point(toolPnl.Location.X - pnlShpB.Width, toolPnl.Location.Y + 140);

                    pnlShpB.Visible = true;
                }
                else
                    pnlShpB.Visible = false;
            };

            //Прозрачность
            Button btnTrnsp = new Button();
            btnTrnsp.BackgroundImage = Image.FromFile(@"buttons\transparency.png", false);
            btnTrnsp.BackgroundImageLayout = ImageLayout.Stretch;
            btnTrnsp.Location = new Point(0, 175);
            btnTrnsp.Height = 30;
            btnTrnsp.Width = 30;
            t.SetToolTip(btnTrnsp, "Transparency");
            btnTrnsp.Tag = toolPnl;

            #region Панель прозрачности
            pnlTrnsp = new Panel();
            pnlTrnsp.BorderStyle = BorderStyle.FixedSingle;
            pnlTrnsp.Height = 30;
            pnlTrnsp.Width = 125;
            pnlTrnsp.Visible = false;

            //Надпись к текстовому полю прозрачности
            Label lblTransp = new Label();
            lblTransp.Text = "Transparency:";
            lblTransp.Width = 80;
            lblTransp.Location = new Point(0, 6);

            //Прозрачность
            NumericUpDown nmrcTransp = new NumericUpDown();
            nmrcTransp.Width = 40;
            nmrcTransp.Location = new Point(80, 4);
            nmrcTransp.KeyUp += delegate
            {
                drawingBrdObjs = true;

                selectObj.Transp = Convert.ToInt32(Math.Round(nmrcTransp.Value / 100 * 255));

                _pictureBox.Invalidate();
            };
            nmrcTransp.ValueChanged += delegate
            {
                drawingBrdObjs = true;

                selectObj.Transp = Convert.ToInt32(Math.Round(nmrcTransp.Value / 100 * 255));

                _pictureBox.Invalidate();
            };
            #endregion

            btnTrnsp.Click += delegate
            {
                nmrcTransp.Text = Math.Round((double)selectObj.Transp / 255 * 100).ToString();

                if (!pnlTrnsp.Visible)
                {
                    Point location = new Point(toolPnl.Location.X + toolPnl.Width, toolPnl.Location.Y + 175);

                    if (location.X + pnlTrnsp.Width < _pictureBox.Width)
                        pnlTrnsp.Location = location;
                    else
                        pnlTrnsp.Location = new Point(toolPnl.Location.X - pnlTrnsp.Width, toolPnl.Location.Y + 175);

                    pnlTrnsp.Visible = true;
                }
                else
                    pnlTrnsp.Visible = false;
            };

            #region Удаление объекта
            Button btnDelB = new Button();
            btnDelB.BackgroundImage = Image.FromFile(@"buttons\delete.png", false);
            btnDelB.BackgroundImageLayout = ImageLayout.Stretch;
            btnDelB.Location = new Point(0, 210);
            btnDelB.Height = 30;
            btnDelB.Width = 30;
            t.SetToolTip(btnDelB, "Delete");
            btnDelB.Tag = toolPnl;
            btnDelB.Click += delegate
            {
                Form formDel = new Form();
                formDel.MinimizeBox = false;
                formDel.MaximizeBox = false;
                formDel.Width = 200;
                formDel.Height = 150;
                formDel.FormBorderStyle = FormBorderStyle.FixedSingle;
                formDel.StartPosition = FormStartPosition.CenterScreen;

                //Предупреждение
                Label lblWarning = new Label();
                lblWarning.Text = "Do you want to delete this object?";
                lblWarning.Width = 180;
                lblWarning.Location = new Point(10, 20);

                Button btnAccpt = new Button();
                btnAccpt.Text = "Yes";
                btnAccpt.Location = new Point(formDel.Width / 2 - 90, formDel.Height - 85);
                btnAccpt.Height = 30;
                btnAccpt.Width = 75;
                btnAccpt.Click += delegate
                {
                    //Проверки на уже используемые кнопки
                    if (mouseInSlctObj)
                        mouseInSlctObj = false;

                    if (toolPnl.Visible)
                        HideToolPanel();

                    //Удаление из dataGridView
                    foreach (DataGridViewRow row in _dataGridView.Rows)
                    {
                        if (row.Cells[0].Value.Equals(selectObj.ID))
                        {
                            _dataGridView.Rows.Remove(_dataGridView.Rows[row.Index]);
                            break;
                        }
                    }

                    //Удаление из списка
                    foreach (var obj in infList)
                    {
                        if (obj.Comparison(selectObj))
                        {
                            infList.Remove(obj);
                            break;
                        }
                    }

                    //Отмена выделения
                    _dataGridView.ClearSelection();

                    selectObj = null;
                    findObj = null;

                    if (!normalMode)
                        normalMode = true;

                    redrawingObjs = true;

                    _pictureBox.Invalidate();

                    formDel.Close();
                };
                Button btnRjct = new Button();
                btnRjct.Text = "No";
                btnRjct.Location = new Point(formDel.Width / 2, formDel.Height - 85);
                btnRjct.Height = 30;
                btnRjct.Width = 75;
                btnRjct.Click += delegate
                {
                    formDel.Close();
                };

                formDel.Controls.Add(lblWarning);
                formDel.Controls.Add(btnAccpt);
                formDel.Controls.Add(btnRjct);

                //Отображение формы
                formDel.ShowDialog();
            };
            #endregion

            #region Добавление объекто на pictureBox
            _pictureBox.Controls.Add(toolPnl);

            toolPnl.Controls.Add(btnClrB);
            _pictureBox.Controls.Add(pnlClrB);
            pnlClrB.Controls.Add(lblClrBR);
            pnlClrB.Controls.Add(nmrcClrBR);
            pnlClrB.Controls.Add(lblClrBG);
            pnlClrB.Controls.Add(nmrcClrBG);
            pnlClrB.Controls.Add(lblClrBB);
            pnlClrB.Controls.Add(nmrcClrBB);
            pnlClrB.Controls.Add(pbClrB);

            toolPnl.Controls.Add(btnWdthB);
            _pictureBox.Controls.Add(pnlWdthB);
            pnlWdthB.Controls.Add(lblBWidth);
            pnlWdthB.Controls.Add(nmrcBWidth);

            toolPnl.Controls.Add(btnDshB);
            _pictureBox.Controls.Add(pnlDshB);
            pnlDshB.Controls.Add(lblDsh);
            pnlDshB.Controls.Add(btnDshYes);
            pnlDshB.Controls.Add(btnDshNo);

            toolPnl.Controls.Add(btnClrOb);
            _pictureBox.Controls.Add(pnlClrO);
            pnlClrO.Controls.Add(lblClrOR);
            pnlClrO.Controls.Add(nmrcClrOR);
            pnlClrO.Controls.Add(lblClrOG);
            pnlClrO.Controls.Add(nmrcClrOG);
            pnlClrO.Controls.Add(lblClrOB);
            pnlClrO.Controls.Add(nmrcClrOB);
            pnlClrO.Controls.Add(pbClrO);

            toolPnl.Controls.Add(btnShp);
            _pictureBox.Controls.Add(pnlShpB);
            pnlShpB.Controls.Add(lblShp);
            pnlShpB.Controls.Add(btnEl);
            pnlShpB.Controls.Add(btnRc);

            toolPnl.Controls.Add(btnTrnsp);
            _pictureBox.Controls.Add(pnlTrnsp);
            pnlTrnsp.Controls.Add(lblTransp);
            pnlTrnsp.Controls.Add(nmrcTransp);

            toolPnl.Controls.Add(btnDelB);
            #endregion
        }


        void ShowToolPanel()
        {
            if (pnlClrB.Visible)
                pnlClrB.Visible = false;

            if (pnlWdthB.Visible)
                pnlWdthB.Visible = false;

            if (pnlDshB.Visible)
                pnlDshB.Visible = false;

            if (pnlClrO.Visible)
                pnlClrO.Visible = false;

            if (pnlShpB.Visible)
                pnlShpB.Visible = false;

            if (pnlTrnsp.Visible)
                pnlTrnsp.Visible = false;

            //Отрисовка панели инструментов
            if (selectObj != null)
            {
                Point location = new Point(selectObj.Rectangle.X + selectObj.Rectangle.Width + 10,
                    selectObj.Rectangle.Y + selectObj.Rectangle.Height - toolPnl.Height);

                if (location.X + toolPnl.Width < _pictureBox.Width)
                {
                    if (location.Y > 0)
                        toolPnl.Location = location;
                    else
                        toolPnl.Location = new Point(selectObj.Rectangle.X + selectObj.Rectangle.Width + 10, 0);

                }
                else
                {
                    if (location.Y > 0)
                    {
                        location = new Point(selectObj.Rectangle.X - toolPnl.Width - 10,
                            selectObj.Rectangle.Y + selectObj.Rectangle.Height - toolPnl.Height);

                        if (location.X > 0)
                            toolPnl.Location = location;
                        else
                        {
                            if (location.Y > 0)
                            {
                                location = new Point(selectObj.Rectangle.Width / 2 + selectObj.Rectangle.X + 10,
                                    (selectObj.Rectangle.Height - toolPnl.Height) / 2 + selectObj.Rectangle.Y);

                                if (location.Y + toolPnl.Height < _pictureBox.Height)
                                    toolPnl.Location = new Point(selectObj.Rectangle.Width / 2 + selectObj.Rectangle.X + 10,
                                    (selectObj.Rectangle.Height - toolPnl.Height) / 2 + selectObj.Rectangle.Y);
                                else
                                    toolPnl.Location = new Point(selectObj.Rectangle.Width / 2 + selectObj.Rectangle.X + 10,
                                    _pictureBox.Height - toolPnl.Height);

                            }
                            else
                                toolPnl.Location = new Point(selectObj.Rectangle.Width / 2 + selectObj.Rectangle.X + 10, 10);
                        }
                    }
                    else
                    {
                        location = new Point(selectObj.Rectangle.X - toolPnl.Width - 10, 0);

                        if (location.X > 0)
                            toolPnl.Location = location;
                        else
                        {
                            if (location.Y > 0)
                                toolPnl.Location = new Point(selectObj.Rectangle.Width / 2 + selectObj.Rectangle.X + 10,
                                (selectObj.Rectangle.Height - toolPnl.Height) / 2 + selectObj.Rectangle.Y);
                            else
                                toolPnl.Location = new Point(selectObj.Rectangle.Width / 2 + selectObj.Rectangle.X + 10, 10);
                        }
                    }
                }

                toolPnl.Visible = true;
            }
        }

        void HideToolPanel()
        {
            if (pnlClrB.Visible)
                pnlClrB.Visible = false;

            if (pnlWdthB.Visible)
                pnlWdthB.Visible = false;

            if (pnlDshB.Visible)
                pnlDshB.Visible = false;

            if (pnlClrO.Visible)
                pnlClrO.Visible = false;

            if (pnlShpB.Visible)
                pnlShpB.Visible = false;

            if (pnlTrnsp.Visible)
                pnlTrnsp.Visible = false;

            toolPnl.Visible = false;
        }
        #endregion

        public MyImage(string imagePath)
        {
            //Создать картинку преобразовать её в хекс
            //Проверить получилась ли картинка в блоке try catch
            if (imagePath != null)
            {
                image = Image.FromFile(imagePath);//Load image

                imageHex = imageToHex(image);
            }
        }

        private Image HexToImage(string imgHex)
        {
            byte[] bytes = null;

            bytes = Enumerable.Range(0, imgHex.Length)//Convert hex to array of bytes
                     .Where(x => x % 2 == 0)
                     .Select(x => Convert.ToByte(imgHex.Substring(x, 2), 16))
                     .ToArray();

            MemoryStream buf = new MemoryStream(bytes);//Create new stream

            Image imageNew = Image.FromStream(buf, true);//Convert hex to Image

            return imageNew;
        }

        private string imageToHex(Image img)
        {
            MemoryStream ms = new MemoryStream();

            img.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);

            byte[] imageBytes = ms.ToArray();

            string imgHex = string.Join("", imageBytes.Select(value => value.ToString("X2")));//Convert array of bytes to hex

            return imgHex;
        }

        private class ImgData
        {
            GraphicsData grData;
            GeneralData gnData;

            private class GraphicsData
            {
                public Rectangle rectangle;
                public bool dash;
                public string shape;
                public int borderWidth;
                public int transp;
                public Color colorObj;
                public Color colorBrd;

                public GraphicsData(Point location)
                {
                    rectangle = new Rectangle(location.X, location.Y, 50, 50);
                    dash = true;
                    shape = "ellipse";
                    borderWidth = 2;
                    transp = 102;
                    colorObj = Color.FromArgb(0, 0, 0);
                    colorBrd = Color.FromArgb(0, 0, 0);
                }
            }
            private class GeneralData
            {
                public string name;
                public int id;
                public static int countID = 0;

                public GeneralData()
                {
                    id = countID;
                    name = "";

                    countID++;
                }
            }

            public Rectangle Rectangle { get => grData.rectangle; set => grData.rectangle = value; }
            public bool Dash { get => grData.dash; set => grData.dash = value; }
            public string Shape { get => grData.shape; set => grData.shape = value; }
            public int BorderWidth { get => grData.borderWidth; set => grData.borderWidth = value; }
            public int Transp { get => grData.transp; set => grData.transp = value; }
            public Color ColorObj { get => grData.colorObj; set => grData.colorObj = value; }
            public Color ColorBrd { get => grData.colorBrd; set => grData.colorBrd = value; }
            public string Name { get => gnData.name; set => gnData.name = value; }
            public int ID { get => gnData.id; set => gnData.id = value; }
            public static int CountID { get => GeneralData.countID; set => GeneralData.countID = value; }

            public ImgData(Point location)
            {
                grData = new GraphicsData(location);
                gnData = new GeneralData();
            }

            public bool Comparison(ImgData obj)
            {
                if (obj != null)
                    if (this.Rectangle == obj.Rectangle && this.Dash == obj.Dash && this.Shape == obj.Shape
                        && this.BorderWidth == obj.BorderWidth && this.Transp == obj.Transp
                        && this.ColorObj == obj.ColorObj && this.Name == obj.Name)
                        return true;

                return false;
            }
        }
    }
}