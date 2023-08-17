namespace Admit_permit
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Windows.Forms;
    using System.Xml;
    using System.Xml.Linq;
    using XrtlExplorer;

        class Admit_Permit
        {
            public static ManualResetEvent allDone = new ManualResetEvent(false);
            public delegate void LockElement();
            public delegate void UnlockElement();
            public delegate void ONClike();
            public delegate void Blokeelement();
            public event LockElement lockElement;
            public event UnlockElement unlockElement;
            public event ONClike onclike;
            public event Blokeelement blokeelement;
            public string accaunt;
            IXrtlExplorer m_xrtlExplorer;
            public static Thread thread1;
            Admit Admit = new Admit();
            public static Thread thread2;
            public string Shift = "";
            public string Date = "";
            public int TIME1 = 0;
            public int TIME2 = 0;
            public int ON4 = 0; // Переменная вкл или выкл
            public int TIME3 = 0;
            public int TIMECOUNT = 0;
            public int t1 = 0; //счетчик на таймер 
            public int TIME4 = 0; // таймер 

            public void Admit_Permit1(IXrtlExplorer xrtlExplorer)
            {
                this.m_xrtlExplorer = xrtlExplorer;
                accaunt = Convert.ToString(xrtlExplorer.GetSystemInfo());
            }
            public Admit_Permit(IXrtlExplorer xrtlExplorer)
            {
                this.m_xrtlExplorer = xrtlExplorer;
                Admit.TopMost = true;
                Admit.Load += (object sender, EventArgs e) => lockElement?.Invoke();
                Admit.FormClosed += (object sender, FormClosedEventArgs e) => FormCount();
                Admit.buttonadmityes.Click += (object sender, EventArgs e) => AnswerTrue();
                Admit.buttonadmitno.Click += (object sender, EventArgs e) => AnswerFalse();

                if (m_xrtlExplorer != null)    // проверка на наличие данных  
                {
                    DateShift();
                    SetTest5();
                    if (ON4 == 1)
                    {
                        accaunt = Convert.ToString(xrtlExplorer.GetSystemInfo());
                        Admit_Permit1(m_xrtlExplorer);
                        TimerStart();
                    }
                }
            }
            public void SetTest5() // получение данных из формы 
            {
                try
                {
                    m_xrtlExplorer.OpenQuery("Test5");
                    TIME1 = Convert.ToInt32(m_xrtlExplorer.GetFieldValue("Test5", "TIME1")); // промежуток времени отправления данных
                    TIME2 = Convert.ToInt32(m_xrtlExplorer.GetFieldValue("Test5", "TIME2")); // промежуток времени очистки данных
                    TIME3 = Convert.ToInt32(m_xrtlExplorer.GetFieldValue("Test5", "TIME3")); // промежуток времени 
                    ON4 = Convert.ToInt32(m_xrtlExplorer.GetFieldValue("Test5", "ON4"));
                    double countBlock = (TIME3 / TIME1);
                    TIME4 = TIME3 / 1000;
                    TIMECOUNT = Convert.ToInt32(countBlock);
                }
                catch (System.Exception)
                {
                    // найти решение по исправлению ошибки 
                }
            }
            public void DateShift()
            {
                m_xrtlExplorer.OpenQuery("DateShift");
                Date = Convert.ToString(m_xrtlExplorer.GetFieldValue("DateShift", "Dat"));
                Shift = Convert.ToString(m_xrtlExplorer.GetFieldValue("DateShift", "Shift"));
                m_xrtlExplorer.CloseQuery("DateShift");
            }
            public void AnswerTrue() // событие обрабатывающее нажатие на кнопку да 
            {
                DialogResult result = MessageBox.Show(  // вызов диалог-окна 
                    "Сохранить дополнительные данные?",
                    "Сообщение",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.DefaultDesktopOnly);
                if (result == DialogResult.Yes)
                {
                    onclike?.Invoke();// событие на нажатие кнопки сохранения дополнительных данных 
                    Thread.Sleep(100);
                    allDone.Reset();
                    Admit.Invoke(new MethodInvoker(() => Admit.Close())); // закрытие форм
                    thread1.Abort();
                    thread2.Abort();
                }
                if (result == DialogResult.No)
                {
                    Admit.Invoke(new MethodInvoker(() => Admit.Close())); // закрытие формы
                    allDone.Reset();
                    thread1.Abort();
                    thread2.Abort();
                }
                SetTest2("Сменное задание по экскаваторам и самосвалам " + Shift + " " + Date, nameperson, namegroup, 1);
            }
            public void AnswerFalse() // Событие обрабатывающее нажатие на кнопку нет 
            {
                SetTest2("Сменное задание по экскаваторам и самосвалам " + Shift + " " + Date, nameperson, namegroup, 0);
                Thread.Sleep(2000);
                Admit.Invoke(new MethodInvoker(() => Admit.Close()));
                unlockElement?.Invoke();
                countForms = 0;
            }
            public void SetTest2(string p_Form_Names, string p_User_Name, string p_Group_user_name, Int16 p_Answer) // Отправка ответа в базу данных  с ответом 
            {
                try
                {
                    m_xrtlExplorer.SetParameter("Test3", "Form_Names", p_Form_Names);
                    m_xrtlExplorer.SetParameter("Test3", "User_Name", p_User_Name);
                    m_xrtlExplorer.SetParameter("Test3", "Group_User_Name", p_Group_user_name);
                    m_xrtlExplorer.SetParameter("Test3", "Answer", p_Answer);
                    m_xrtlExplorer.ExecSQL("Test3", "update");
                }
                catch (System.Exception)
                {
                    // blokeelement?.Invoke();
                    thread1.Abort();
                    thread2.Abort();
                }
            }
            public void TimerStart()
            {
                DataPerson(accaunt); // серилизация xml для получения имени и группы пользователя
                Test4(); // очистка устаревших данных
                SetTest1("Сменное задание по экскаваторам и самосвалам " + Shift + " " + Date, nameperson, namegroup);  // отправка данных            
                SetTest3("Сменное задание по экскаваторам и самосвалам " + Shift + " " + Date, nameperson, namegroup);  // получение данных 
                allDone.Set();
                thread1 = new Thread(Timer2_Tick);
                thread1.Name = "1";
                thread1.Start();
                allDone.Set();
                thread2 = new Thread(Timer3_Tick);
                thread2.Name = "2";
                thread2.Start();
            }
            public int countForms;  // счетчик на закрытие и открытие формы 
            public string nameperson = ""; // переменная которая держит в себе имя сотрудника
            public string namegroup = ""; // переменная которая держит в себе наименование группы сотрудников 
            public void DataPerson(string accaunt) // метод который получает имя и группу пользователя
            {
                string variable = accaunt;
                variable = variable.Replace("< ", "<");
                XDocument xDocument = new XDocument();
                xDocument = XDocument.Parse(variable);
                Dictionary<string, string> valuePairs = new Dictionary<string, string>();
                foreach (XElement xElement in xDocument.Descendants())
                {
                    foreach (XAttribute xAttribute in xElement.Attributes())
                        valuePairs.Add(xElement.Name + " " + xAttribute.Name, xAttribute.Value);
                }
                foreach (KeyValuePair<string, string> keyValue in valuePairs)
                {
                    string key = keyValue.Key;
                    string value = keyValue.Value;

                    if (key == "User name")
                    {
                        nameperson = keyValue.Value;
                    }
                    if (key == "Group name")
                    {
                        namegroup = keyValue.Value;
                    }
                }
            }
            private void Timer2_Tick() // таймер на 5 секунд 
            {
                try
                {
                    if (m_xrtlExplorer != null)   // проверка на наличие данных 
                    {
                        SetTest1("Сменное задание по экскаваторам и самосвалам " + Shift + " " + Date, nameperson, namegroup);  // отправка данных            
                        SetTest3("Сменное задание по экскаваторам и самосвалам " + Shift + " " + Date, nameperson, namegroup);  // получение данных 
                        Thread.Sleep(TIME1);
                        Timer2_Tick();
                    }
                }
                catch (ThreadAbortException)
                {
                    //MessageBox.Show(Convert.ToString(ex));
                    // найти решение по исправлению ошибки 
                }
            }
            public void FormCount() // счетчик на минус 
            {
                countForms = 0;
            }
            public void Timer3_Tick() // таймер на очискту данных раз в 10 секунд 
            {
                try
                {
                    if (m_xrtlExplorer != null)
                    {
                        Test4();   // Очистка устаревших данных 
                        Thread.Sleep(TIME2);
                        Timer3_Tick();
                    }
                }
                catch (ThreadAbortException)
                {
                    // найти решение по исправлению ошибки 
                }
            }
            public void Test4() // полное обновление 10 секунд
            {
                try
                {
                    m_xrtlExplorer.ExecSQL("Test4", "update");
                }
                catch (System.Exception)
                {
                    // найти решение по исправлению ошибки 
                }
            }

            public void SetTest1(string p_Form_Names, string p_User_Name, string p_Group_user_name) // отправка данных в базу данных
            {
                var v = m_xrtlExplorer.GetXrtlControls();
                try
                {
                    m_xrtlExplorer.SetParameter("Test1", "Form_Names", p_Form_Names);
                    m_xrtlExplorer.SetParameter("Test1", "User_Name", p_User_Name);
                    m_xrtlExplorer.SetParameter("Test1", "Group_User_Name", p_Group_user_name);
                    m_xrtlExplorer.ExecSQL("Test1", "update");
                }
                catch (System.Exception)
                {
                    //    blokeelement?.Invoke();
                    //   MessageBox.Show(Convert.ToString(ex));
                    thread1.Abort();
                    thread2.Abort();
                }
            }
            public void SetTest3(string p_Form_Names, string p_User_Name, string p_Group_user_name) // получение данных из формы 
            {
                try
                {
                    m_xrtlExplorer.SetParameter("Test2", "Form_Names", p_Form_Names);
                    m_xrtlExplorer.SetParameter("Test2", "User_Name", p_User_Name);
                    m_xrtlExplorer.SetParameter("Test2", "Group_User_Name", p_Group_user_name);
                    m_xrtlExplorer.OpenQuery("Test2");
                    string name1 = Convert.ToString(m_xrtlExplorer.GetFieldValue("Test2", "NAME1"));
                    Treatment(name1);
                }
                catch (System.Exception ex)
                {
                    //MessageBox.Show(Convert.ToString(ex));
                }
            }
            public int countBlock; // счетчик 
            public void TimeBlock(string message)   // метод который считает количество циклов с вопросом 
            {
                if (t1 == 0)
                {
                    TIME4 = TIME3 / 1000;
                    Admit.labeltimer2.Invoke((MethodInvoker)(() => Admit.labeltimer2.Text = " " + TIME4));
                }
                if (message == "Question")
                {
                    countBlock++;
                    Admit.labeltimer2.Invoke((MethodInvoker)(() => Admit.labeltimer2.Text = " " + (TIME4)));
                    TIME4 = TIME4 - TIME1 / 1000;
                    t1++;
                }

                if (countBlock == TIMECOUNT)  // 60 секунд 
                {
                    Admit.Invoke(new MethodInvoker(() => Admit.Close()));
                    blokeelement?.Invoke();
                    allDone.Reset();
                    SetTest2("Сменное задание по экскаваторам и самосвалам " + Shift + " " + Date, nameperson, namegroup, 1);
                    thread1.Abort();
                    thread2.Abort();
                }

                if (message != "Question")
                {
                    countBlock = 0;
                    t1 = 0;
                }
            }
            public void Treatment(string p_name1) // наш метод обработки событий 
            {
                if (p_name1 != "")
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(p_name1);
                    XmlElement root = doc.DocumentElement;
                    string message = "";
                    string message1 = "";
                    if (root.HasAttribute("type"))
                    {
                        message = root.GetAttribute("type");
                    }
                    if (root.HasAttribute("value"))
                    {
                        message1 = root.GetAttribute("value");
                    }
                    if (message != "null") // если код не равняется нулю 
                    {
                        if (countForms == 0 && message != "answerTrue") // внесено изменение 
                        {
                            Thread _thread = new Thread(() =>
                            {
                                try
                                {
                                    Admit.ShowDialog();
                                    Admit.TopMost = true;
                                }
                                catch (System.Exception)
                                {
                                    // найти решение по исправлению ошибки 
                                }
                            });
                            _thread.SetApartmentState(ApartmentState.STA);
                            _thread.Start();
                            countForms++;
                        }

                        if (message == "Question")
                        {
                            if (Admit.countini)
                            {
                                Admit.labeladmit.Invoke((MethodInvoker)(() => Admit.labeladmit.Text = "Отдать форму этому сотруднику " + message1 + " "));
                                Admit.buttonadmityes.Invoke((MethodInvoker)(() => Admit.buttonadmityes.Visible = true));
                                Admit.buttonadmitno.Invoke((MethodInvoker)(() => Admit.buttonadmitno.Visible = true));
                                blokeelement?.Invoke();
                                TimeBlock(message);
                                Admit.labeltimer1.Invoke((MethodInvoker)(() => Admit.labeltimer1.Visible = true));
                                Admit.labeltimer2.Invoke((MethodInvoker)(() => Admit.labeltimer2.Visible = true));
                            }
                        }
                        if (message == "answerFalse")
                        {
                            if (Admit.countini)
                            {
                                Admit.labeladmit.Invoke((MethodInvoker)(() => Admit.labeladmit.Text = "вам отказал сотрудник " + message1 + " "));
                                Thread.Sleep(2000); // время для отображения
                                Admit.Invoke(new MethodInvoker(() => Admit.Close()));
                                blokeelement?.Invoke();
                                allDone.Reset();
                                thread1.Abort();
                                thread2.Abort();
                            }
                        }
                        if (message == "answerTrue")
                        {
                            if (Admit.countini)
                            {
                                Admit.labeladmit.Invoke((MethodInvoker)(() => Admit.labeladmit.Text = "вам передал управление " + message1 + " "));
                                Admit.labeladmit.Invoke((MethodInvoker)(() => Admit.labeladmit.Text = " "));
                                Admit.Invoke(new MethodInvoker(() => Admit.Close()));
                                countForms = 0;
                                unlockElement?.Invoke();
                            }
                        }
                        if (message == "block")
                        {
                            if (Admit.countini)
                                Admit.labeladmit.Invoke((MethodInvoker)(() => Admit.labeladmit.Text = "Форма занята " + message1 + " "));
                        }
                        if (message == "answerWait")
                        {
                            if (Admit.countini)
                                Admit.labeladmit.Invoke((MethodInvoker)(() => Admit.labeladmit.Text = "Ожидание ответа от " + message1 + " "));
                        }
                        if (message == "answerBlock")
                        {
                            if (Admit.countini)
                                Admit.labeladmit.Invoke((MethodInvoker)(() => Admit.labeladmit.Text = "Ожидание ответа от " + message1 + " "));
                        }
                    }

                    else // если сигнал будет равен нулю то мы закрываем форму и не мешаем работе 
                    {
                        if (countForms == 1)
                        {
                            if (Admit.countini)
                            {
                                Admit.Invoke(new MethodInvoker(() => Admit.Close()));
                                Admit.labeladmit.Invoke((MethodInvoker)(() => Admit.labeladmit.Text = ""));
                                unlockElement?.Invoke();
                            }
                        }
                        countForms = 0;
                    }
                }
            }
        }
}