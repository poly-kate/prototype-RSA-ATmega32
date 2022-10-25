#define F_CPU 8000000UL
#include <avr/io.h>
#include <util/delay.h>
#include <avr/interrupt.h>
#include <avr/eeprom.h>
#include <math.h>


register unsigned char IT asm("r16");
volatile unsigned char done;
volatile unsigned char IDX;


int part_protocol=0;//1,2,3
int num;
int param;
int error=0;
int zero_count;
int lg=0x90;
int key_mas[100];
uint8_t key_count;//общее колво ключей
int error_count=0;//количество ключей не найденных в eeprom
int current_number;//номер текущего ключа в eeprom

uint32_t n, e, d;



void writeSerial(char str)//отправка
{
		while(!(UCSRA&(1<<UDRE))){};
		UDR = str;
}


void main_funct_1(void)//обработка приема команды
{
	num=num*2+3;
	writeSerial(num);
	//команда принята	
}



uint32_t encrypt(uint32_t n, uint32_t e, uint32_t m)//шифрование
{
	uint32_t c;
	uint32_t tmp = 1;
	uint32_t i;
	for (i = 0; i < e; i++)
	{
		tmp = (tmp * m) % n;
	}
	
	c=tmp;
	return c;
}



uint8_t decrypt(uint32_t n, uint32_t e, uint16_t m)//дешифровка
{
	
		uint32_t c;
		uint32_t tmp = 1;
		uint32_t i;
		for (i = 0; i < e; i++)
		{
			tmp = (tmp * m) % n;
		}
		
		c=tmp;
		return c;
	
}


void text_convert(uint32_t n, uint32_t e, uint32_t d)//обработка файла 1-4
{
	//num - операция
	//current_number-номер текущего блока
	switch (num)
	{
		case 1://шифрование
		//из одного байта получаем два
		{
			zero_count=0;
			key_mas[current_number*4+1]++;
			uint8_t siym;
			int g=0x70;
			while (zero_count<3)
			{
				while(!(UCSRA&(1<<RXC))) {};
				siym=UDR;

				if (siym==48) zero_count++;
				
				uint16_t out = encrypt(n,e, siym);//два байта
				
				
				uint16_t buf=0b0000000011111111;
				uint8_t out1 = out&buf; //младшие 8 бит
				
				uint8_t out2=out>>8;//старшие 8 бит
				g++;g++;
			
				writeSerial(out1);
				writeSerial(out2);
			}
			zero_count=0;
			break;
		}
		
		
		case 2://дешифровка
		//из двух байт получаем один
		{
			zero_count=0;
			key_mas[current_number*4+2]++;
			while(!(UCSRA&(1<<RXC))) {};
			uint32_t sim1, sim2;
			
			while (zero_count<3)//объединяем два байта в 32-16
			{
				while(!(UCSRA&(1<<RXC))) {};
				sim1=UDR;
				while(!(UCSRA&(1<<RXC))) {};
				sim2=UDR;
				uint16_t sim3 = (sim2<<8)+sim1;
				
				uint8_t out = decrypt(n, d, sim3);
				if (out==48) zero_count++;
				writeSerial(out);
			}
			zero_count=0;	
			break;
		}
		
		case 3://подписание
		{
			
			zero_count=0;
			key_mas[current_number*4+3]++;
			uint8_t siym;
			while (zero_count<3)
			{
				while(!(UCSRA&(1<<RXC))) {};
				siym=UDR;
				if (siym==48) zero_count++;
				
				uint16_t out = encrypt(n,d, siym);//два байта
				uint16_t buf=0b0000000011111111;
				uint8_t out1 = out&buf; //младшие 8 бит
				uint8_t out2=out>>8;//старшие 8 бит
				writeSerial(out1);
				writeSerial(out2);
			}
			zero_count=0;
			break;
		}
		
		case 4://проверка подписи
		//из двух байт получаем один
		{
			zero_count=0;
					
			key_mas[current_number*4+4]++;
			//eeprom_write_byte (99, key_mas[current_number*4+4]);
			while(!(UCSRA&(1<<RXC))) {};
			uint32_t sim1, sim2;
					
			while (zero_count<3)//объединяем два байта в 32-16
			{
				while(!(UCSRA&(1<<RXC))) {};
				sim1=UDR;
				while(!(UCSRA&(1<<RXC))) {};
				sim2=UDR;
				uint16_t sim3 = (sim2<<8)+sim1;
				uint8_t out = decrypt(n, e, sim3);
				if (out==48) zero_count++;
				writeSerial(out);
			}
			zero_count=0;
			break;
		}

		default:
		break;
		
	}
}


uint32_t key5()//получение открытого ключа
{
	//4 byte
	int k=0;
	uint32_t a,b, c, d;
	
	while(k<4)
	{
		while(!(UCSRA&(1<<RXC))) {};
		if (k==0) a = UDR;
		else if (k==1) b = UDR;
		else if (k==2) c = UDR;
		else if (k==3) d = UDR;
		k++;
	}
	k=0;
	uint32_t key=0+(d<<32)+(c<<16)+(b<<8)+a;
	return key;
}
int research(uint32_t n1, uint32_t e1, uint32_t d1)//поиск ключа в eeprom
{
	key_count= eeprom_read_byte (1);//в ячейке 1 хранится кол-во записанных ключей
	uint32_t eep_n, eep_e, eep_d;
	for(int i=0; i<key_count;i++)
	{
		int numer=16*i+2;//начало нового набора
		eep_n=eeprom_read_dword (numer);
		if(eep_n==n1)//совпал первый ключ
		{
			eep_e=eeprom_read_dword (numer+4);
			if(eep_e==e1)//совпал второй ключ
			{
				eep_d=eeprom_read_dword (numer+8);
				d=eep_d;
				current_number=i;
				return(1);
			}
		}
	}
	return(0);//не найден
}



int main_eepr_write(uint32_t n, uint32_t e, uint32_t d)//записать ключ в еепром
{
	//на каждый выделяется 16 байт, 4*3+4
	// key_count общее колво записанных ключей, новый пишем в конец
	
	int new_index=16*key_count+2;
	eeprom_write_dword (new_index, n);
	eeprom_write_dword (new_index+4, e);
	eeprom_write_dword (new_index+8, d);
	
	//обнуляем счетчики
	eeprom_write_byte (new_index+12, 0);
	eeprom_write_byte (new_index+13, 0);
	eeprom_write_byte (new_index+14, 0);
	eeprom_write_byte (new_index+15, 0);
	
	key_count++;
	current_number=key_count-1;
	eeprom_write_byte (1,key_count);
		
	return(1);
}

int obrabotka(uint32_t n, uint32_t e, uint32_t d)
{
	if (d==0)//поиск в существующих ключах
	{
		if (research(n, e, d)==1)//ключ найден
		{
			//увеличить счетчик и передать управления нужной команде
			return(1);
		}
		else //ключ не найден
		{
			error_count++;
			return(0);
		}
	}
	else//запись в EEPROM
	{
		main_eepr_write(n,e,d);
		return(1);
	}
	
}


int obrabotka5_7(int type, int parametr)
{
	
	if (type==5)//удалить ключ номер parametr
	{
		parametr--;
		//удалить ключ из массива и еепром
		//key_count-общее число ключей
		
		if (parametr+1==key_count)
		{
			
			int a=4*parametr;
			for (int i=1; i<5; i++)
			{
				key_mas[a+i]=0;
			}
			//===============================
			int ind=16*parametr+2;
			
			for (int i=0; i<16; i++)
			{
				eeprom_write_byte(ind+i,0xFF);
			}
					
		}
		else
		{
			int endd = 4*(key_count-1)+1;
			for (int s=4*parametr+1; s<endd;s++)
			{
				key_mas[s]=key_mas[s+4];
			}
			key_mas[endd]=0;
			key_mas[endd+1]=0;
			key_mas[endd+2]=0;
			key_mas[endd+3]=0;
			//================================
			int ind=16*parametr+2;
			for (int i=0; i<16*(key_count-parametr+1); i++)
			{
				eeprom_write_byte(ind+i,eeprom_read_byte(ind+i+16));

			}		
			
		}
		key_count--;
		eeprom_write_byte(1, key_count);
		writeSerial(1);
		return 1;			
	}
	else if (type==6)
	{
		if(parametr==1)//отправить на мк статистику обычную
		{
			writeSerial(key_count);//количество ключей в массиве
			//отправить ключи, потом статистику
			writeSerial(error_count);
			for (int i=0; i<key_count; i++)//для каждого ключа
			{
				for(int j=2; j<10; j++)
				{
					writeSerial(eeprom_read_byte(16*i+j));
				}
				for(int l=1; l<5;l++)
				{
					writeSerial(key_mas[4*i+l]);
				}
			}
			
			return 1;
		}
		else if(parametr==2)//сохранить обычную статистику в еепром
		{
			//error_count ненайденные в нулевую ячейку
			
			for(int i=0; i<key_count; i++)
			{
				
				for (int f=0; f<4; f++)
				{
					eeprom_write_byte(16*i+14+f,key_mas[4*i+f+1]);//ячейка, значение
				}	
			}
			
			writeSerial(1);
			return 1;
			
		}
		else if(parametr==3)//обнулить обычную статистику
		{
			for(int i=0; i<100; i++)
			{
				key_mas[i]=0;	
			}
			writeSerial(3);
			return 1;
		}
		else return 0;
	}
	else return 0;
}





ISR(USART_RXC_vect)//обработка прерывания
{
	
	if (part_protocol==0)//принимаем тип операции-всегда корректен
	{
		while(!(UCSRA&(1<<RXC))) {};
		num=UDR;	
		int num2=num*2+3;
		writeSerial(num2);
		part_protocol++;
		return;
	}

	else if(part_protocol==1)//принимаем параметры команды
	{
		if ((num>0)&&(num<5))//первые 4 команды - принимаем три ключа
		{
			 n = key5();
			 e = key5();
			 d = key5();
			//вернуть мк 1-успех 0-ошибка
			if(obrabotka(n,e,d)==0)
			{
				writeSerial(25);
				//полный сброс
				part_protocol=0;
				n=0; e=0; d=0;
				return;
			}
			else writeSerial(1);
			part_protocol++;
			return;
		}
		else //все остальные команды++
		{
			while(!(UCSRA&(1<<RXC))) {};
			param=UDR; 
			
			writeSerial(param);
			//writeSerial(46);
			obrabotka5_7(num, param);
			part_protocol=0;//???????
			//clean
			return;
		}
	}
	else if(part_protocol==2)//принимаем текст 1-4
	{
		
		text_convert(n,  e,  d);
		part_protocol=0;
		return;
		
	}
	return;
}

void blink13(uint8_t count)
{
	PORTB |= (1<<PB5);
	count =(count <<1);count--; //count=(count*2)-1;
	for (IT=0;IT<count;IT++)
	{
		_delay_ms(500);
		PORTB ^= (1<<PB5);
	};
};

int main(void)
{
	// USART init
	UBRRL=51;
	UCSRB=(1<<TXEN)|(1<<RXEN)|(1<<RXCIE);
	UCSRC=(1<<URSEL)|(3<<UCSZ0);

	DDRB |= (1<<PB5);	
	key_count=eeprom_read_byte (1);//количество ключей в еепром
	if (key_count==0xFF) key_count=0;
	for (int i=0; i<100; i++) {key_mas[i]=0;}//массив статистика
	

	
	blink13(3); //ready indication
	IDX=0;
	done=0;
	sei();
	//setup();

	for (;;)
	{
		if (done)
		{
			PORTB |= (1<<PB5);
			PORTB &= ~(1<<PB5);
			done=0;
		}
	}
	return 0;
}




