using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Test.Models;

namespace Test.ViewModel
{
    class RiPViewModel : INotifyPropertyChanged
    {
        RipContext context; //Контекст данных

        private ObservableCollection<Company> companys;
        public ObservableCollection<Company> Companys
        {
            get { return companys; }
            set
            {
                //При изменении нашей коллекции вызывает соотсвестующее событие, чтобы datagrid обновился
                companys = value;
                OnPropertyChanged();
            }
        }

        private Company selectedCompany;
        public Company SelectedCompany
        {
            get { return selectedCompany; }
            set {
                selectedCompany = value;
                if (selectedCompany != null) //Если выбранная компания не "пуста" то устанавливаем коллекцию пользователей выбранной компании
                    SelectedCompanyUsers = new ObservableCollection<Users>(selectedCompany.Users);
                else
                    SelectedCompanyUsers = null;
            }
        }

        private ObservableCollection<Users> selectedCompanyUsers; //Коллекция пользователей выбранной компании
        public ObservableCollection<Users> SelectedCompanyUsers
        {
            get { return selectedCompanyUsers; }
            set
            {
                selectedCompanyUsers = value;
                //Вызываем событие, чтобы datagrid, отображаюий пользователей выбранной компании обновился
                OnPropertyChanged();
            }
        }

        private Users setectedUser;
        public Users SelectedUser
        {
            get { return setectedUser; }
            set
            {
                setectedUser = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        public RiPViewModel()
        {
            LoadFromDataBase();
        }

        //Команда удаления компании
        private RelayCommand removeCompanyCommand;
        public RelayCommand RemoveCompanyCommand
        {
            get
            {
                return removeCompanyCommand ??
                    (removeCompanyCommand = new RelayCommand(obj =>
                    {
                        try
                        {
                            if (selectedCompany != null)
                            {
                                context.Company.Remove(selectedCompany);
                                Companys.Remove(selectedCompany);
                                context.SaveChanges();
                            }
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message);
                            LoadFromDataBase();
                        }
                    },
                    (obj => SelectedCompany != null)));
            }
        }


        //Команда удаления пользователей
        private RelayCommand removeUser;
        public RelayCommand RemoveUser
        {
            get
            {
                return removeUser ??
                    (removeUser = new RelayCommand(obj =>
                    {
                        try
                        {   
                            if (selectedCompany != null)
                            {   
                                //Удаляем выбранного пользователя, и сохраняем изменения
                                context.Users.Remove(SelectedUser);
                                SelectedCompanyUsers.Remove(SelectedUser);
                                context.SaveChanges();
                            }
                        }
                        catch (Exception e)
                        {   //Если по какой-то причине операция прошла неуспешно, то выводим сообщение с исключением. 
                            //Так же загружаем в программу корректные данные из базы данных
                            MessageBox.Show(e.Message);
                            LoadFromDataBase();
                        }
                    },
                    (obj => SelectedUser != null))); //Команда активна, только если выбран какой-то пользователь
            }
        }


        //Команда обновления компаний
        private RelayCommand updateCompanys;
        public RelayCommand UpdateCompanys
        {
            get
            {
                return updateCompanys ??
                    (updateCompanys = new RelayCommand(obj =>
                    {
                        try
                        {   //Если мы добавляли компании, то они будут добавлены в коллекцию, при этом в контексте данных их не будет
                            //Поэтому добавляем их сами
                            //Проверяем количество записей в контексте и в нашей текущей коллекции
                            if (Companys.Count > context.Company.Count())
                            {
                                int ContectCount = context.Company.Count();
                                for (int i = ContectCount; i < Companys.Count; i++) //И добавляем все новые записи
                                {
                                    context.Company.Add(Companys[i]);
                                }
                            }
                            //Сохраняем изменения
                            //Изменения сделанные уже в существующих записях обновятся сами
                            context.SaveChanges();
                        }
                        catch (Exception e)
                        {
                            //Если что-то пошло не так, то выводим сообщение и загружаем последние корректные данные с базы данных
                            MessageBox.Show(e.Message);
                            LoadFromDataBase();
                        }
                    }));
            }
        }

        //Команда обновления пользователей.
        //С её вызовом вносятся изменения в существующих пользователей и/или добавляются новые
        private RelayCommand updateUsers;
        public RelayCommand UpdateUsers
        {
            get
            {
                return updateUsers ??
                    (updateUsers = new RelayCommand(obj =>
                    {
                        try
                        {   
                            if (SelectedCompanyUsers.Count > context.Company.Find(selectedCompany.Id).Users.Count)
                            {
                                int ContextCount = context.Company.Find(selectedCompany.Id).Users.Count;
                                for (int i = ContextCount; i < SelectedCompanyUsers.Count; i++)
                                {
                                    SelectedCompanyUsers[i].Company = SelectedCompany.Id;
                                    context.Users.Add(SelectedCompanyUsers[i]);
                                }
                            }
                            context.SaveChanges();
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message);
                            LoadFromDataBase();

                        }
                    },
                    //Команда активна, только если в текущем контекте данных выбранную компанию.
                    //Таким образом, если мы выбрали компанию и измени какие-либо даные о ней, или же добавили новую компанию
                    //Но при этом не подтвердили изменения и не отправили их в бд, работать с пользователями данной компании будет нельзя
                    (obj => FindInDbSet())));
            }
        }

        //Функция для загрузки данных из базы
        private void LoadFromDataBase()
        {
            context = new RipContext(); //Создаём/Пересоздаём контекст данных
            Companys = new ObservableCollection<Company>(context.Company.ToList()); //Добавляем его в коллекцию
            //"Очищаем" выбранную компанию и выбранного пользователя
            SelectedCompany = null;
            SelectedUser = null;
        }

        //Данная функция пытается найти в DbSet выбранную компанию
        //И возвращает true, если находит.
        private bool FindInDbSet()
        {
            if (SelectedCompany != null)
            {
                bool exist = context.Company.Any(info =>
                info.Id == SelectedCompany.Id && info.Name == SelectedCompany.Name &&
                info.ContractStatus == SelectedCompany.ContractStatus);
                return exist;
            }
             return false;
        }
    }
}
