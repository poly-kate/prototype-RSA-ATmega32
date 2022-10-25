using System;
using System.IO;
using System.IO.Ports;
using System.Security.Cryptography;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication3
{
    public partial class Form1 : Form
    {
        int typefunct = 0;//не выбрано основное действие
                          /*0 - не выбрано 1 - шифрование 2 - расшифрование
                            3 - ВХ и эл. подписи 4 - ВХ и проверка эл. подписи (5 - ошибка ввода) */
        
        int baud = 5; //скорость передачи, по умолчанию 9600
        string addres; //адрес файла
        string hesh_addres;
        int n;//ок
        int ee;//е
        int d;//зк
        int number2;
        int number3;
        int key_number;//номер открытого ключа
        int param = 0;//парметр действий с 5го
        int error;
        int gener = 0;
        byte[] FileBuffer;
        long sizefile;
        byte[] FileBuffer_hesh;
        long sizefilehesh;
        string port_status="ЗАКРЫТ";
       
        System.IO.Ports.SerialPort Serial;
        //=======================================================

        public Form1()
        {
            InitializeComponent();
            comboBox4.Items.Clear();
            foreach (string portName in SerialPort.GetPortNames())
            {
                comboBox4.Items.Add(portName);
            }
            comboBox4.SelectedIndex = 0;
        }
       

        private void comboBox4_SelectedIndexChanged_1(object sender, EventArgs e)//порт
        {
            String name = ((string)comboBox4.SelectedItem);
            Serial = new SerialPort(name, 9600, System.IO.Ports.Parity.None, 8, StopBits.One);
             Serial.Encoding = Encoding.UTF8;
                    }
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)//скорость передачи
        {
            if (comboBox2.SelectedItem == comboBox2.Items[0]) baud = 600;
            else if (comboBox2.SelectedItem == comboBox2.Items[1]) baud = 1200;
            else if (comboBox2.SelectedItem == comboBox2.Items[2]) baud = 2400;
            else if (comboBox2.SelectedItem == comboBox2.Items[3]) baud = 4800;
            else if (comboBox2.SelectedItem == comboBox2.Items[4]) baud = 9600;
            else if (comboBox2.SelectedItem == comboBox2.Items[5]) baud = 14400;
            else if (comboBox2.SelectedItem == comboBox2.Items[6]) baud = 19200;
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)//выбор действия
        {
            if (comboBox3.SelectedItem == comboBox3.Items[0]) typefunct = 1;
            else if (comboBox3.SelectedItem == comboBox3.Items[1]) typefunct = 2;
            else if (comboBox3.SelectedItem == comboBox3.Items[2]) typefunct = 3;
            else if (comboBox3.SelectedItem == comboBox3.Items[3]) typefunct = 4;

            if ((typefunct == 2) || (typefunct == 4))//неактивно при 2,4
            {
               
                gener = 0;
                checkBox1.Enabled = false;
            }
            else checkBox1.Enabled = true;
        }

        private void textBox3_TextChanged(object sender, EventArgs e)//адресная строка
        {
            addres = textBox3.Text;
            
        }
        
      
        private void textBox5_TextChanged(object sender, EventArgs e)//number3
        {
            number3 = int.Parse(textBox5.Text);
        }
        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)//получить статистику
        {
            typefunct = 6;
            param = 1;
            main_send();
        }
        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)//сохранить статистику
        {
            typefunct = 6;
            param = 2;
            main_send();
        }
        private void linkLabel4_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)//обнулить статистику
        {
            typefunct = 6;
            param = 3;
            main_send();
        }
       
        private void button2_Click(object sender, EventArgs e)//выполнить 1-4
        {
            if ((typefunct > 4) || (typefunct < 1))
            {
                label5.Text = "Не указана операция";
                return;
            }  
            else
            {
                if (gener==0)//указано вручную
                {
                    

                    d = 0;//сигнал
                    label5.Text = "Для использования собственного открытого ключа необходимо удостовериться в его корректности";
                    if((n==0)||(ee==0))
                    {
                        label5.Text = "Не указан ключ";
                        return;
                    }
                }
                else
                {
                    key_generation();
                    label5.Text = "  ";
                    //gener = 0;              
                }
                if (File.Exists(addres) == false)//неверный файл
                {
                    label5.Text = "Неверно указан файл";
                    return;
                }
                else
                {
                    if (typefunct==4)
                    {
                        if (File.Exists(hesh_addres) == false)//неверный файл
                        {
                            label5.Text = "Неверно указан файл с цифровой подписью";
                            return;
                        }
                        else
                        {
                            FileBuffer_hesh = File.ReadAllBytes(hesh_addres);
                            sizefilehesh = new System.IO.FileInfo(hesh_addres).Length;
                        }
                    }
                    FileBuffer = File.ReadAllBytes(addres);
                    sizefile = new System.IO.FileInfo(addres).Length;
                }
                main_send();
            }
        }
    
     

        private void button4_Click(object sender, EventArgs e)//удалить ключ номер...
        {
            if (number3 == 0)
            {
                label5.Text = "Не указан параметр";
                return;
            }
            typefunct = 5;
            param = number3;
            main_send();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)//галочка
        {
            
            gener = 1;
        }

        private void textBox2_TextChanged_1(object sender, EventArgs e)
        {
            if (textBox2.Text != "")
            {
                n = int.Parse(textBox2.Text);
            }
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {
            if (textBox6.Text != "")
            {
                ee = int.Parse(textBox6.Text);
            }
           
        }
        
        

        private void key_generation()//генерация открытого и закрытого ключа
        {
            int[] pq = { 13, 17, 19, 23, 29, 31, 37, 41, 43, 47 };
                        //53, 59, 61, 67, 71, 73, 79, 83, 89, 97};//20
            Random r = new Random();
            int pIndex = r.Next(1, 10);
            int qIndex = r.Next(1, 10);
            if (pIndex == qIndex) pIndex--;
            int p = pq[pIndex];
            int q = pq[qIndex];
            n = p * q; 
            int eiler = (p - 1) * (q - 1);//ф-я эйлера
            ee = funct_e(eiler);//вычисляем открытый ключ
            //найдено e и ф-я эйлера
            d = funct_d(ee, eiler);//вычисляем закрытый ключ
        }

        private int funct_e(int eiler)//ищем е
        {
            //1<e<eiler-1 ; е взаимно простое с eiler
            Random rand = new Random();
            int new_ee;
            do
            {
               new_ee = rand.Next(1, eiler-1);
            }while (NOD(new_ee, eiler)!=1);
            return new_ee;
        }

        private int NOD(int a, int b)//нужно чтобы НОД==1 для взаимной простоты a и b
        {
            int c = a % b;
            while (c!=0)
            {
                a = b;
                b = c;
                c = a % b;
            }
            return b; //возвращает НОД
        }

        private int funct_d(int e1, int eiler)//поиск закрытого ключа 
        {
            int new_d;
            //(d*e)%eiler==1
            int sum = 0;
            int k = 0;
            do
            {
                k++;
                sum += eiler;
            } while ((sum+1)%e1!=0);
            sum += 1;
            sum /= e1;
            new_d = sum;
            return new_d;
        }

        private byte[] ComputeMD5Checksum(string path)
        {
            using (FileStream fs = System.IO.File.OpenRead(path))
            {
                MD5 md5 = new MD5CryptoServiceProvider();
                byte[] fileData = new byte[fs.Length];
                fs.Read(fileData, 0, (int)fs.Length);
                byte[] checkSum = md5.ComputeHash(fileData);

                return checkSum;
            }
        }
  private void main_send()
        {
          
            Serial.Open();
            label6.Text = "ОТКРЫТ";
            
            //посылаем на МК номер команды 
            byte[] t1 = BitConverter.GetBytes(typefunct);
            Serial.Write(t1, 0, 1);
           // Serial.DiscardOutBuffer();
            MessageBox.Show(typefunct.ToString());
            
            //принимаем с МК ответ - проверка
            int t2 = Serial.ReadByte();
          //  Serial.DiscardInBuffer();
            MessageBox.Show(t2.ToString());
            if (t2 != typefunct * 2 + 3)
            {
                label5.Text = "Сбой отправки команды. Попробуйте еще раз";
                Serial.Close();
                label6.Text = "ЗАКРЫТ";
                return;
            }
            //мк принял команду

            if ((typefunct > 0) && (typefunct < 5))//основные 4 команды - отправляем ключи
            {

                byte[] b_n = BitConverter.GetBytes(n);
                byte[] b_e = BitConverter.GetBytes(ee);
                byte[] b_d = BitConverter.GetBytes(d);

                Serial.Write(b_n, 0, 4);
                MessageBox.Show(n.ToString());
                Serial.Write(b_e, 0, 4);
                MessageBox.Show(ee.ToString());

                Serial.Write(b_d, 0, 4);
                
                MessageBox.Show(d.ToString());

                int z = Serial.ReadByte();
                if (gener == 0)//если ключ указан вручную, проверить есть ли он в eeprom
                {
                    
                    if (z == 25)//ключ не найден
                    {
                        label5.Text = "Неизвестный ключ. Используйте другой.";
                        Serial.Close();
                        label6.Text = "ЗАКРЫТ";
                        return;
                    }
                }

                //передача файла               

                switch (typefunct)
                {
                    case 1:
                        {
                            //посылаем 1 байт - получаем 2
                            int j = 0;
                            byte[] newBufFile = null;
                            newBufFile = new byte[sizefile * 2 + 6];
                            for (int i = 0; i < sizefile; i++)
                            {
                                Serial.Write(FileBuffer, i, 1);//отправляем
                                newBufFile[j] = Convert.ToByte(Serial.ReadByte());
                                newBufFile[j + 1] = Convert.ToByte(Serial.ReadByte());
                                j++; j++;
                            }
                            for (int i = 0; i < 3; i++)
                            {
                                Serial.Write("0");
                                newBufFile[j] = Convert.ToByte(Serial.ReadByte());
                                newBufFile[j + 1] = Convert.ToByte(Serial.ReadByte());
                                j++; j++;
                            }

                            File.WriteAllBytes("D:\\out.txt", newBufFile);
                            break;
                        }
                    case 2:
                        {
                            //посылаем 2 байта - получаем 1
                            int j = 0;
                            byte[] newBufFile = null;
                            newBufFile = new byte[sizefile / 2-3];//выходной файл
                            for (int i = 0; i < sizefile; i++)
                            {
                                Serial.Write(FileBuffer, i, 1);//отправляем1
                                i++;
                                Serial.Write(FileBuffer, i, 1);//отправляем2

                                byte ji = Convert.ToByte(Serial.ReadByte());
                                if (j < sizefile / 2 - 3) newBufFile[j] = ji;
                                j++;
                            }




                            File.WriteAllBytes("D:\\out2.txt", newBufFile);
                            break;
                        }
                    case 3:
                        {
                            //вычисление хэша и электронной подписи
                            byte[] hesh = new byte[16];
                            hesh = ComputeMD5Checksum(addres);

                            //MessageBox.Show(hesh.ToString());

                            //========================================
                            //посылаем 1 байт - получаем 2

                            int j = 0;

                            byte[] newBufFile = null;
                            newBufFile = new byte[16 * 2 + 6];
                            for (int i = 0; i < 16; i++)
                            {
                                Serial.Write(hesh, i, 1);//отправляем

                                newBufFile[j] = Convert.ToByte(Serial.ReadByte());
                                newBufFile[j + 1] = Convert.ToByte(Serial.ReadByte());
                                j++; j++;
                            }

                            for (int i = 0; i < 3; i++)
                            {
                                Serial.Write("0");
                                newBufFile[j] = Convert.ToByte(Serial.ReadByte());
                                newBufFile[j + 1] = Convert.ToByte(Serial.ReadByte());
                                j++; j++;
                            }
                            File.WriteAllBytes("D:\\out3.txt", newBufFile);

                            byte[] newBufFile2 = null;
                            newBufFile2 = new byte[sizefile * 2 + 6];
                            newBufFile2 = File.ReadAllBytes("D:\\out3.txt");
                            break;
                        }
                    case 4:
                        {
                            //посылаем 2 байта - получаем 1
                            //посылаем и расшифровываем сумму, вычисляем снова хэш и сравниваем 

                            byte[] hesh = new byte[16];//здесь оригинальная сумма
                            hesh = ComputeMD5Checksum(addres);

                            //====================================================================
                            int j = 0;
                            byte[] newBufFile = null;
                            newBufFile = new byte[sizefilehesh / 2 + 1];
                            for (int i = 0; i < sizefilehesh; i++)
                            {
                                Serial.Write(FileBuffer_hesh, i, 1);//отправляем1
                                i++;
                                Serial.Write(FileBuffer_hesh, i, 1);//отправляем2

                                byte ji = Convert.ToByte(Serial.ReadByte());
                                if (j < sizefilehesh / 2 - 3) newBufFile[j] = ji;
                                j++;
                            }

                            File.WriteAllBytes("D:\\out4.txt", newBufFile);//здесь расшифрованная сумма
                            int result_compare = 1;
                            for (int i = 0; i < 16; i++)
                            {
                                if (newBufFile[i] != hesh[i]) result_compare = 0;
                            }

                            byte[] newBufFile2 = null;
                            newBufFile2 = new byte[sizefile / 2 + 1];
                            newBufFile2 = File.ReadAllBytes("D:\\out4.txt");

                            if (result_compare == 1) MessageBox.Show("Электронная подпись верна!");
                            else MessageBox.Show("Электронная подпись неверна!");
                            break;
                        }
                    default: break;


                }

                Serial.DiscardOutBuffer();
                Serial.DiscardInBuffer();
                Serial.Close();
                label6.Text = "ЗАКРЫТ";

            }
   else//в остальных случаях параметр 1 байт
            {
                byte[] c = BitConverter.GetBytes(param);
                Serial.Write(c, 0, 1);
                Serial.DiscardOutBuffer();
                MessageBox.Show(param.ToString());
                //отправили параметр для 5 это номер ключа, для 6-подномер 1-3
                //проверка
                //MessageBox.Show((Serial.BytesToRead).ToString());
                int t3 = Serial.ReadByte();
                MessageBox.Show(t3.ToString());
                if (t3 != param)
                {
                    label5.Text = "Сбой отправки параметра. Попробуйте еще раз";
                    return;
                }
                //здесь успешно отправили параметр операции

                switch (typefunct)
                {
                        case 5://удалить из мк ключ №...
                        {
                            //номер - param
                            int res1 = Serial.ReadByte();
                            if (res1 == 1)//успешно
                            {
                                MessageBox.Show("Ключ удален!");
                            }
                            else
                            {
                                label5.Text = "Произошла ошибка";
                                return;
                            }
                            Serial.DiscardOutBuffer();
                            Serial.DiscardInBuffer();
                            Serial.Close();
                            label6.Text = "ЗАКРЫТ";
                            break;
                        }
                        case 6:
                        {
                            switch (param)
                            {
                                    case 1://получить статистику
                                    {
                                        string p_n = null, p_e = null;

                                        int countofkey = Serial.ReadByte();//общее кол-во ключей
                                                                           
                                        MessageBox.Show(countofkey.ToString());

                                        int error_key = Serial.ReadByte();
                                        MessageBox.Show(error_key.ToString());//не найденные ключи
                                        File.AppendAllText("D:\\out5.txt", error_key.ToString()+"\n");
                                        //ДОБАВИТЬ В ВЫХОДНОЙ ФАЙЛ
                                        string kv = " - ";

                                        int[] newBufFile = null;
                                        newBufFile = new int[20];

                                        /////////////ДЛЯ ВСЕХ
                                        for (int j = 0; j < countofkey; j++)
                                        {
                                            string gotov;
                                            //  newBufFile[0] = (j + 1);
                                            for (int i = 0; i < 2; i++)//принимаем два открытых ключа
                                            {
                                                int a, b, cc, d, bbb;
                                                a = Serial.ReadByte();
                                                b = Serial.ReadByte();
                                                cc = Serial.ReadByte();
                                                d = Serial.ReadByte();
                                                bbb = 0 + (d << 32) + (cc << 16) + (b << 8) + a;
                                                if (i == 0) p_n = bbb.ToString();
                                                else p_e = bbb.ToString();
                                            }
                                            gotov = (j+1).ToString()+kv+p_n + kv + p_e;

                                            for (int l = 0; l < 4; l++)//принимаем статистику 4 байта
                                            {
                                                gotov += kv + (Serial.ReadByte().ToString());
                                            }
                                            gotov += "\n";
                                            File.AppendAllText("D:\\out5.txt", gotov);

                                        }
                                        File.AppendAllText("D:\\out5.txt", "---------------------------------\n");
                                        Serial.DiscardOutBuffer();
                                        Serial.DiscardInBuffer();
                                        Serial.Close();
                                        label6.Text = "ЗАКРЫТ";
                                        break;
                                    }
                                    case 2://сохранить статистику в eeprom
                                    {
                                        int pop = Serial.ReadByte();
                                        MessageBox.Show(pop.ToString());
                                        Serial.DiscardOutBuffer();
                                        Serial.DiscardInBuffer();
                                        Serial.Close();
                                        label6.Text = "ЗАКРЫТ";
                                        break;
                                    }
                                    case 3:
                                    {
                                        //обнуление стастики
                                        int pop = Serial.ReadByte();
                                        if (pop != 3)//ошибка
                                        {
                                            label5.Text = "Сбой обнуления. Попробуйте еще раз";
                                            return;
                                        }
                                        Serial.DiscardOutBuffer();
                                        Serial.DiscardInBuffer();
                                        Serial.Close();
                                        label6.Text = "ЗАКРЫТ";
                                        break;
                                    }
                                    default: break;
                            }
                            Serial.Close();
                            label6.Text = "ЗАКРЫТ";
                            break;
                        }
                        default:
                        {
                            Serial.Close();
                            label6.Text = "ЗАКРЫТ";
                            break;
                        }

                }

            }

           
        }

        private void textBox1_TextChanged(object sender, EventArgs e)//адрес файла с готовым хэшем
        {
            hesh_addres = textBox1.Text;
        }
    }
}

